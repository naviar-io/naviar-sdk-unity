using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public enum LocalizationModeType { TEXTURE, FEATURES, BOTH };

    /// <summary>
    /// API VPS Service
    /// </summary>
    public class VPSLocalisationService : MonoBehaviour
    {
        private const string defaultUrl = "https://vps.naviar.io/";

        [Tooltip("Start VPS in OnAwake")]
        public bool StartOnAwake;
        [SerializeField]
        private KeyCode toggleFreeFlightMode;

        [Header("Providers")]
        [Tooltip("Which camera, GPS and tracking use for runtime")]
        public ServiceProvider RuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for mock data")]
        public ServiceProvider MockProvider;
        private ServiceProvider provider;

        [Tooltip("Use mock provider when VPS service has started")]
        public bool UseMock = false;
        [Tooltip("Always use mock provider in Editor, even if UseMock is false")]
        public bool ForceMockInEditor = true;

        [Header("Default VPS Settings")]
        [Tooltip("Send features or photo")]
        public LocalizationModeType LocalizationMode;
        [Tooltip("Send GPS")]
        public bool SendGPS;
        [Tooltip("Localization fails count to reset VPS session")]
        public int FailsCountToReset = 5;

        [Header("Location Settings")]
        public string[] locationsIds;

        [Header("Debug")]
        [Tooltip("Save images in request localy before sending them to server")]
        [SerializeField]
        private bool saveImagesLocaly;

        private VPSPrepareStatus vpsPreparing;
        private FreeFlightSimulation freeFlightSimulation;

        /// <summary>
        /// Event localisation error
        /// </summary>
        public event System.Action<ErrorInfo> OnErrorHappend;

        /// <summary>
        /// Event localisation success
        /// </summary>
        public event System.Action<LocationState> OnPositionUpdated;

        /// <summary>
        /// Event mobile vps is downloaded
        /// </summary>
        public event System.Action OnVPSReady;

        /// <summary>
        /// Event of change angle from correct to incorrect and back
        /// </summary>
        public event System.Action<bool> OnCorrectAngle;

        private VPSLocalisationAlgorithm algorithm;

        private IEnumerator Start()
        {
            if (!provider)
                yield break;

            freeFlightSimulation = FindObjectOfType<FreeFlightSimulation>();
            if (freeFlightSimulation == null)
                Debug.Log("FreeFlightSimulation is not found on scene");

            if (locationsIds == null || locationsIds.Length == 0)
            {
                Debug.LogError("LocationsIds array is null or empty. It must have at least one value");
                yield break;
            }

            if (LocalizationMode != LocalizationModeType.TEXTURE)
            {
                yield return DownloadMobileVps();
            }

            if (StartOnAwake)
                StartVPS();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause || !Application.isEditor)
                return;

            ResetTracking();
        }

        /// <summary>
        /// Start VPS service with default settings
        /// </summary>
        public void StartVPS()
        {
            SettingsVPS defaultSettings = new SettingsVPS(locationsIds, FailsCountToReset);
            StartVPS(defaultSettings);
        }

        /// <summary>
        /// Start VPS service with settings
        /// </summary>
        public void StartVPS(SettingsVPS settings)
        {
            if (freeFlightSimulation != null && freeFlightSimulation.IsActive())
            {
                StartCoroutine(LocalizeWithDelay(freeFlightSimulation.GetLocalizationDelay()));
                return;
            }

            StopVps();
            provider.InitGPS(SendGPS);
            provider.ResetSessionId();

            if (LocalizationMode != LocalizationModeType.TEXTURE)
            {
                StartCoroutine(DownloadMobileVps());
                if (!IsReady())
                {
                    VPSLogger.Log(LogLevel.DEBUG, "MobileVPS is not ready. Start downloading...");
                    return;
                }
            }
            algorithm = new VPSLocalisationAlgorithm(defaultUrl, this, provider, settings, LocalizationMode, SendGPS);

            algorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            algorithm.OnLocalisationHappend += (ls) => OnPositionUpdated?.Invoke(ls);
            algorithm.OnCorrectAngle += (correct) => OnCorrectAngle?.Invoke(correct);
        }

        /// <summary>
        /// Stop VPS service
        /// </summary>
        public void StopVps()
        {
            algorithm?.Stop();
        }

        /// <summary>
        /// Get latest localisation result
        /// </summary>
        public LocationState GetLatestPose()
        {
            if (freeFlightSimulation != null && freeFlightSimulation.IsActive())
            {
                return GetLocationStateFromCurrentPose();
            }

            if (algorithm == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "VPS service is not running. Use StartVPS before");
                return null;
            }
            return algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Was there at least one successful localisation?
        /// </summary>
        public bool IsLocalized()
        {
            return provider.GetTracking().GetLocalTracking().IsLocalisedLocation;
        }

        /// <summary>
        /// Get download mobileVPS progress (between 0 and 1)
        /// </summary>
        public float GetPreparingProgress()
        {
            return vpsPreparing.GetProgress();
        }

        /// <summary>
        /// Is mobileVPS ready?
        /// </summary>
        public bool IsReady()
        {
            return vpsPreparing?.GetProgress() == 1;
        }

        /// <summary>
        /// Reset current tracking
        /// </summary>
        public void ResetTracking()
        {
            if (!provider)
                return;

            provider.GetARFoundationApplyer()?.ResetTracking();
            provider.GetTracking().ResetTracking();
            VPSLogger.Log(LogLevel.NONE, "Tracking reseted");
        }

        /// <summary>
        /// Switch on / off free flight mode 
        /// </summary>
        public void SetFreeFlightMode(bool isFreeFlight)
        {
            if (freeFlightSimulation == null)
            {
                Debug.Log("FreeFlightSimulation is not found on VPS");
                return;
            }

            freeFlightSimulation.SetFreeFlightMode(isFreeFlight);
            if (isFreeFlight)
                StopVps();
            else
                ResetTracking();

            FindObjectOfType<FakeCamera>()?.SetMockImageEnable(!isFreeFlight);
        }

        /// <summary>
        /// Localize camera in mockPosition and mockRotation
        /// </summary>
        /// <param name="mockPosition"></param>
        /// <param name="mockRotation"></param>
        public void MockLocalize(Vector3 mockPosition, Quaternion mockRotation)
        {
            MockLocalize(mockPosition, mockRotation.eulerAngles);
        }

        /// <summary>
        /// Localize camera in mockPosition and mockRotation
        /// </summary>
        /// <param name="mockPosition"></param>
        /// <param name="mockRotation"></param>
        public void MockLocalize(Vector3 mockPosition, Vector3 mockRotation)
        {
            LocalisationResult localisationResult = new LocalisationResult();
            localisationResult.VpsPosition = mockPosition;
            localisationResult.VpsRotation = mockRotation;
            localisationResult.TrackingPosition = provider.GetTracking().GetLocalTracking().Position;
            localisationResult.TrackingRotation = provider.GetTracking().GetLocalTracking().Rotation.eulerAngles;
            localisationResult.LocalitonId = locationsIds[0];

            LocationState locationState = new LocationState();
            locationState.Status = LocalisationStatus.VPS_READY;
            locationState.Localisation = localisationResult;

            MockProvider.GetARFoundationApplyer().ApplyVPSTransform(localisationResult, true);
            MockProvider.GetTracking().Localize(locationsIds[0]);

            StopVps();

            OnPositionUpdated?.Invoke(locationState);
        }

        private void Awake()
        {
            DebugUtils.SaveImagesLocaly = saveImagesLocaly;

            // check what provider should VPS use
            var isMockMode = UseMock || Application.isEditor && ForceMockInEditor;
            provider = isMockMode ? MockProvider : RuntimeProvider;

            if (!provider)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't load proveder! Select provider for VPS service!");
                return;
            }

            ServiceProvider[] providers = GetComponentsInChildren<ServiceProvider>();
            foreach (var provider in providers)
            {
                provider.gameObject.SetActive(false);
            }
            provider.gameObject.SetActive(true);
            vpsPreparing = new VPSPrepareStatus();
        }

        private IEnumerator DownloadMobileVps()
        {
            vpsPreparing.OnVPSReady += () => OnVPSReady?.Invoke();
            if (!IsReady())
            {
                yield return vpsPreparing.DownloadNeurals();
            }
            provider.InitMobileVPS();
        }

        private LocationState GetLocationStateFromCurrentPose()
        {
            LocationState locationState = new LocationState();
            locationState.Status = LocalisationStatus.VPS_READY;

            LocalisationResult localisationResult = new LocalisationResult();
            localisationResult.LocalitonId = provider.GetTracking().GetLocalTracking().LocationId;
            localisationResult.VpsPosition = provider.GetTracking().GetLocalTracking().Position;
            localisationResult.VpsRotation = provider.GetTracking().GetLocalTracking().Rotation.eulerAngles;

            locationState.Localisation = new LocalisationResult();

            return locationState;
        }

        private IEnumerator LocalizeWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LocationState locationState = GetLocationStateFromCurrentPose();
            provider.GetTracking().Localize(locationState.Localisation.LocalitonId);
            OnPositionUpdated?.Invoke(locationState);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleFreeFlightMode))
            {
                SetFreeFlightMode(!freeFlightSimulation.IsActive());
            }
        }
    }
}