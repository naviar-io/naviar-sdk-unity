using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        [Header("Providers")]
        [Tooltip("Which camera, GPS and tracking use for runtime")]
        public ServiceProvider RuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for mock data")]
        public ServiceProvider MockProvider;
        private ServiceProvider provider;

        private SettingsVPS currentSettings;

        [Tooltip("Use mock provider when VPS service has started")]
        public bool UseMock = false;
        [Tooltip("Always use mock provider in Editor, even if UseMock is false")]
        public bool ForceMockInEditor = true;

        [SerializeField]
        private KeyCode toggleFreeFlightMode = KeyCode.Tab;

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
        [SerializeField]
        private bool saveLogsInFile;

        private VPSPrepareStatus vpsPreparing;
        private FreeFlightSimulationAlgorithm freeFlightSimulationAlgorithm;

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

        private bool isDefaultAlgorithm = true;
        private ILocalisationAlgorithm algorithm;

        private IEnumerator Start()
        {
            if (!provider)
                yield break;

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

            currentSettings = settings;

            SwitchLocalizationAlgorithm(isDefaultAlgorithm);

            algorithm.Run();
        }

        private void ConfigureAlgorithmListeners(ILocalisationAlgorithm localisationAlgorithm)
        {
            localisationAlgorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            localisationAlgorithm.OnLocalisationHappend += (ls) => OnPositionUpdated?.Invoke(ls);
            localisationAlgorithm.OnCorrectAngle += (correct) => OnCorrectAngle?.Invoke(correct);
        }

        private void SwitchLocalizationAlgorithm(bool isDefault)
        {
            isDefaultAlgorithm = isDefault;

            StopAllCoroutines();
            freeFlightSimulationAlgorithm.gameObject.SetActive(!isDefaultAlgorithm);
            if (isDefaultAlgorithm)
            {
                algorithm = new VPSLocalisationAlgorithm(defaultUrl, this, provider, currentSettings, LocalizationMode, SendGPS);
                ConfigureAlgorithmListeners(algorithm);
            }
            else
            {
                freeFlightSimulationAlgorithm.SetSettings(currentSettings);
                algorithm = freeFlightSimulationAlgorithm;
            }
        }

        /// <summary>
        /// Stop VPS service
        /// </summary>
        public void StopVps()
        {
            algorithm?.Stop();
        }

        /// <summary>
        /// Pause work VPS service without resetting the current session
        /// </summary>
        public void PauseVPS()
        {
            algorithm?.Pause();
        }

        /// <summary>
        /// Resume work VPS service with current session
        /// </summary>
        public void ResumeVPS()
        {
            algorithm?.Resume();
        }

        /// <summary>
        /// Get latest localisation result
        /// </summary>
        public LocationState GetLatestPose()
        {
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
            return provider.GetTracking().IsLocalized();
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

        public SessionInfo GetCurrentSessionInfo()
        {
            return provider.GetSessionInfo();
        }

        private void Awake()
        {
            DebugUtils.SaveImagesLocaly = saveImagesLocaly;
            VPSLogger.WriteLogsInFile = saveLogsInFile;

            freeFlightSimulationAlgorithm = FindObjectOfType<FreeFlightSimulationAlgorithm>(true);
            if (freeFlightSimulationAlgorithm == null)
            {
                Debug.Log("FreeFlightSimulationAlgorithm not found. FreeFlightMode is not available");
            }
            ConfigureAlgorithmListeners(freeFlightSimulationAlgorithm);

            // check what provider should VPS use
            var isMockMode = UseMock || Application.isEditor && ForceMockInEditor;
            provider = isMockMode ? MockProvider : RuntimeProvider;

            if (!provider)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't load provider! Select provider for VPS service!");
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

        private void Update()
        {
            if (Input.GetKeyDown(toggleFreeFlightMode))
            {
                SwitchLocalizationAlgorithm(!isDefaultAlgorithm);
            }
        }
    }
}