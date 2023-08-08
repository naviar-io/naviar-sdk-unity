using System.Collections;
using System.Collections.Generic;
using naviar.VPSService.JSONs;
using UnityEngine;
using System.IO;
using TensorFlowLite;
using Unity.Collections;
using System;

namespace naviar.VPSService
{
    /// <summary>
    /// Internal management VPS
    /// </summary>
    public class VPSLocalisationAlgorithm : ILocalisationAlgorithm
    {
        private VPSLocalisationService localisationService;
        private ServiceProvider provider;

        private LocationState locationState;

        private SettingsVPS settings;

        private LocalizationModeType localizationMode;

        IRequestVPS requestVPS = new UnityWebRequestVPS();

        /// <summary>
        /// Event localisation error
        /// </summary>
        public event System.Action<ErrorInfo> OnErrorHappend;

        /// <summary>
        /// Event localisation success
        /// </summary>
        public event System.Action<LocationState> OnLocalisationHappend;

        /// <summary>
        /// Event of change angle from correct to incorrect and back
        /// </summary>
        public event System.Action<bool> OnCorrectAngle;

        float neuronTime = 0;

        int failsToReset;
        int currentFailsCount = 0;
        bool isLocalization = true;
        bool isCorrectAngle = true;
        bool isPaused = false;

        #region Metrics

        int attemptCount;
        private const string FullLocalizationStopWatch = "FullLocalizationStopWatch";
        private const string TotalWaitingTime = "TotalWaitingTime";
        private const string TotalInferenceTime = "TotalInferenceTime";

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vps_service">Parent GameObject, for start coroutine</param>
        /// <param name="vps_provider">Provider to get camera, gps and tracking</param>
        /// <param name="vps_settings">Settings</param>
        public VPSLocalisationAlgorithm(string url, VPSLocalisationService vps_servise, ServiceProvider vps_provider, SettingsVPS vps_settings, LocalizationModeType localizationMode,
                                        bool sendGps)
        {
            requestVPS.SetUrl(url);
            localisationService = vps_servise;

            provider = vps_provider;

            this.localizationMode = localizationMode;
            failsToReset = vps_settings.failsCountToResetSession;

            var gps = provider.GetGPS();
            if (gps != null)
                gps.SetEnable(sendGps);

            settings = vps_settings;

            locationState = new LocationState();

            neuronTime = 0;

            currentFailsCount = 0;
            isLocalization = true;
            isPaused = false;

            OnErrorHappend += (error) => ResetIfFails(failsToReset);
        }

        public void Run()
        {
            localisationService.StartCoroutine(LocalisationRoutine());
        }

        public void Stop()
        {
            provider.GetMobileVPS()?.StopTask();
            localisationService.StopAllCoroutines();
            ARFoundationCamera.semaphore.Free();
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// Get latest available Location state (updated in LocalisationRoutine())
        /// </summary>
        /// <returns></returns>
        public LocationState GetLocationRequest()
        {
            return locationState;
        }

        /// <summary>
        /// Main cycle. Check readiness every service, send request, apply the resulting localization if success
        /// </summary>
        /// <returns>The routine.</returns>
        private IEnumerator LocalisationRoutine()
        {
#if !UNITY_EDITOR
            bool isARInitialized = false;
            yield return localisationService.StartCoroutine(ARAvailabilityChecking.StartChecking((status) => isARInitialized = status));

            if (!isARInitialized)
            {
                ErrorInfo error = new ErrorInfo(ErrorCode.AR_NOT_SUPPORTED, "AR is not supported on current device");
                OnErrorHappend?.Invoke(error);
                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
                yield break;
            }
#endif

            attemptCount = 0;
            MetricsCollector.Instance.StartStopwatch(FullLocalizationStopWatch);

            Texture2D Image;
            string Meta;
            byte[] Embedding;

            var camera = provider.GetCamera();
            if (camera == null)
            {
                ErrorInfo error = new ErrorInfo(ErrorCode.NO_CAMERA, "Camera is not available");
                OnErrorHappend?.Invoke(error);
                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
                yield break;
            }

            MobileVPS mobileVPS = provider.GetMobileVPS();
            switch(localizationMode)
            {
                case LocalizationModeType.TEXTURE:
                    camera.Init(new VPSTextureRequirement[] { provider.GetTextureRequirement() });
                    break;
                case LocalizationModeType.FEATURES:
                    camera.Init(new VPSTextureRequirement[] { mobileVPS.imageFeatureExtractorRequirements, mobileVPS.imageEncoderRequirements });
                    break;
                case LocalizationModeType.BOTH:
                    camera.Init(new VPSTextureRequirement[] { mobileVPS.imageFeatureExtractorRequirements, mobileVPS.imageEncoderRequirements, provider.GetTextureRequirement() });
                    break;
            }

            var tracking = provider.GetTracking();
            if (tracking == null)
            {
                ErrorInfo error = new ErrorInfo(ErrorCode.TRACKING_NOT_AVALIABLE, "Tracking is not available");
                OnErrorHappend?.Invoke(error);
                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
                yield break;
            }

            var arRFoundationApplyer = provider.GetARFoundationApplyer();

            while (true)
            {
                while (isPaused)
                    yield return null;

                while (!camera.IsCameraReady())
                    yield return null;

                MetricsCollector.Instance.StartStopwatch(TotalWaitingTime);

                do
                {
                    if (isCorrectAngle != CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles, settings))
                    {
                        isCorrectAngle = !isCorrectAngle;
                        OnCorrectAngle?.Invoke(isCorrectAngle);
                    }
                    if (!isCorrectAngle)
                        yield return null;
                } while (!isCorrectAngle);

                MetricsCollector.Instance.StopStopwatch(TotalWaitingTime);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", TotalWaitingTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(TotalWaitingTime));

                var metaMsg = DataCollector.CollectData(provider, settings.locationIds);
                Meta = DataCollector.Serialize(metaMsg);

                switch (localizationMode)
                {
                    // if send features - send them
                    case LocalizationModeType.FEATURES:
                    case LocalizationModeType.BOTH:
                        while (!ARFoundationCamera.semaphore.CheckState())
                            yield return null;
                        ARFoundationCamera.semaphore.TakeOne();

                        NativeArray<byte> featureExtractorInput = camera.GetBuffer(mobileVPS.imageFeatureExtractorRequirements);
                        if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                        {
                            VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for FeatureExtractor");
                            yield return null;
                            continue;
                        }

                        if (DebugUtils.SaveImagesLocaly)
                        {
                            VPSLogger.Log(LogLevel.VERBOSE, "Saving FeatureExtractor image before sending...");
                            DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, "features");
                        }

                        NativeArray<byte> encoderInput = camera.GetBuffer(mobileVPS.imageEncoderRequirements);
                        if (encoderInput == null || encoderInput.Length == 0)
                        {
                            VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for Encoder");
                            yield return null;
                            continue;
                        }

                        if (DebugUtils.SaveImagesLocaly)
                        {
                            VPSLogger.Log(LogLevel.VERBOSE, "Saving Encoder image before sending...");
                            DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, "encoder");

                            DebugUtils.SaveJson(metaMsg);
                        }

                        while (mobileVPS.ImageFeatureExtractorIsWorking || mobileVPS.ImageEncoderIsWorking)
                            yield return null;

                        var preprocessTask = mobileVPS.StartPreprocess(featureExtractorInput, encoderInput);
                        while (!preprocessTask.IsCompleted)
                            yield return null;

                        if (!preprocessTask.Result)
                        { 
                            yield return null;
                            continue;
                        }

                        var imageFeatureExtractorTask = mobileVPS.GetFeaturesAsync();
                        var imageEncoderTask = mobileVPS.GetGlobalDescriptorAsync();

                        MetricsCollector.Instance.StartStopwatch(TotalInferenceTime);

                        while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                            yield return null;

                        MetricsCollector.Instance.StopStopwatch(TotalInferenceTime);
                        TimeSpan neuronTS = MetricsCollector.Instance.GetStopwatchTimespan(TotalInferenceTime);
                        neuronTime = neuronTS.Seconds + neuronTS.Milliseconds / 1000f;

                        VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", TotalInferenceTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(TotalInferenceTime));

                        ARFoundationCamera.semaphore.Free();
                        Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores,
                            imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);

                        if (DebugUtils.SaveImagesLocaly)
                        {
                            VPSLogger.Log(LogLevel.VERBOSE, "Saving embeding before sending...");
                            DebugUtils.SaveDebugEmbd(Embedding);
                        }

                        VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");

                        if (localizationMode == LocalizationModeType.FEATURES)
                        {
                            localisationService.StartCoroutine(requestVPS.SendVpsRequest(Embedding, Meta, () => ReceiveResponce(tracking, arRFoundationApplyer)));
                        }
                        else
                        {
                            Image = camera.GetFrame(provider.GetTextureRequirement());
                            localisationService.StartCoroutine(requestVPS.SendVpsRequest(Image, Embedding, Meta, () => ReceiveResponce(tracking, arRFoundationApplyer)));
                        }
                        break;
                    // if not - send only photo and meta
                    case LocalizationModeType.TEXTURE:
                        Image = camera.GetFrame(provider.GetTextureRequirement());

                        if (DebugUtils.SaveImagesLocaly)
                        {
                            VPSLogger.Log(LogLevel.VERBOSE, "Saving image before sending...");
                            DebugUtils.SaveDebugImage(Image);
                            DebugUtils.SaveJson(metaMsg);
                        }

                        VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");
                        localisationService.StartCoroutine(requestVPS.SendVpsRequest(Image, Meta, () => ReceiveResponce(tracking, arRFoundationApplyer)));
                        break;
                }

                if (isLocalization)
                    yield return new WaitForSeconds(settings.localizationTimeout - neuronTime); 
                else
                    yield return new WaitForSeconds(settings.calibrationTimeout - neuronTime);
            }
        }

        private void ReceiveResponce(ITracking tracking, ARFoundationApplyer arRFoundationApplyer)
        {
            if (isPaused)
                return;

            attemptCount++;

            locationState = requestVPS.GetLocationState(); 

            if (locationState.Status == LocalisationStatus.VPS_READY)
            {
#region Metrics
                if (!tracking.IsLocalized())
                {
                    MetricsCollector.Instance.StopStopwatch(FullLocalizationStopWatch);

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", FullLocalizationStopWatch, MetricsCollector.Instance.GetStopwatchSecondsAsString(FullLocalizationStopWatch));
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] SerialAttemptCount {0}", attemptCount);
                }

#endregion

                bool changeLocationId = tracking.Localize(locationState.Localisation.LocationId);
                if (changeLocationId)
                    provider.ResetSessionId();

                locationState.Localisation = arRFoundationApplyer?.ApplyVPSTransform(locationState.Localisation, isLocalization);

                isLocalization = false;
                currentFailsCount = 0;

                OnLocalisationHappend?.Invoke(locationState);
                VPSLogger.Log(LogLevel.NONE, "VPS localization successful");
            }
            else
            {
                OnErrorHappend?.Invoke(locationState.Error);
                VPSLogger.LogFormat(LogLevel.NONE, "VPS Request Failed: {0}", locationState.Error.LogDescription());
            }
        }

        private void ResetIfFails(int countToReset)
        {
            if (isLocalization)
                return;

            currentFailsCount++;
            if (currentFailsCount >= countToReset)
            {
                currentFailsCount = 0;
                provider.ResetSessionId();
                isLocalization = true;
            }
        }

        public bool CheckTakePhotoConditions(Vector3 curAngle, SettingsVPS settings)
        {
            return (curAngle.x < settings.MaxAngleX || curAngle.x > 360 - settings.MaxAngleX) &&
            (curAngle.z < settings.MaxAngleZ || curAngle.z > 360 - settings.MaxAngleZ);
        }
    }
}