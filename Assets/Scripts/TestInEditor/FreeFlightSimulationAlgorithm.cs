using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace naviar.VPSService
{
    public class FreeFlightSimulationAlgorithm : MonoBehaviour, ILocalisationAlgorithm
    {
        private VPSLocalisationService localisationService;
        private ServiceProvider provider;
        
        private SettingsVPS settings;

        [Header("Free Flight UI")]
        [SerializeField]
        private Canvas freeFlightCanvasPrefab;

        private GameObject freeFlightCanvas;

        [Header("Fake localization")]
        [SerializeField]
        private KeyCode simulateLocalizationButton = KeyCode.E;
        public MockLocalization mockLocalization;

        [Header("Localization delay in free flight mode")]
        [SerializeField]
        private float localizationDelay;

        bool isPaused = false;
        bool isCorrectAngle = true;

        [Header("Moving")]
        [SerializeField]
        private float speed;
        [SerializeField]
        private float accelerationRatio;
        [SerializeField]
        private KeyCode moveForwardButton = KeyCode.W;
        [SerializeField]
        private KeyCode moveLeftButton = KeyCode.A;
        [SerializeField]
        private KeyCode moveBackButton = KeyCode.S;
        [SerializeField]
        private KeyCode moveRightButton = KeyCode.D;
        [SerializeField]
        private KeyCode AccelerationButtton = KeyCode.LeftShift;
        [SerializeField]
        private KeyCode switchCursorLockButton = KeyCode.F;

        [Header("Camera rotation")]
        [SerializeField]
        private float sensitivity;
        [SerializeField]
        private float maxVerticalAngle = 45;
        private Vector2 turn;

        private Transform player;

        public event System.Action<LocationState> OnLocalisationHappend;
        public event System.Action<ErrorInfo> OnErrorHappend;
        public event System.Action<bool> OnCorrectAngle;

        private void Awake()
        {
            XROrigin xrOrigin = FindObjectOfType<XROrigin>();
            provider = FindObjectOfType<ServiceProvider>();
            localisationService = FindObjectOfType<VPSLocalisationService>();

            if (xrOrigin == null)
            {
                Debug.LogError("ARSessionOrigin is not found on scene");
                return;
            }
            player = xrOrigin.Camera.transform;

            freeFlightCanvas = Instantiate(freeFlightCanvasPrefab).gameObject;
        }

        private void Update()
        {   
            if (Input.GetKeyDown(simulateLocalizationButton))
                MockLocalize(mockLocalization.locationId, mockLocalization.position, mockLocalization.rotation);

            if (Input.GetKeyDown(switchCursorLockButton))
            {
                switch (Cursor.lockState)
                {
                    case CursorLockMode.Locked:
                        Cursor.lockState = CursorLockMode.None;
                        break;
                    case CursorLockMode.None:
                        Cursor.lockState = CursorLockMode.Locked; 
                        break;
                }
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                RotateCamera();
                MoveCamera();
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            freeFlightCanvas?.SetActive(true);
            FindObjectOfType<FakeCamera>()?.SetMockImageEnable(false);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            freeFlightCanvas?.SetActive(false);
            FindObjectOfType<FakeCamera>()?.SetMockImageEnable(true);
        }

        private void RotateCamera()
        {
            turn.x += Input.GetAxis("Mouse X") * sensitivity;
            turn.y += Input.GetAxis("Mouse Y") * sensitivity;
            if (turn.y > maxVerticalAngle)
                turn.y = maxVerticalAngle;
            else if (turn.y < -maxVerticalAngle)
                turn.y = -maxVerticalAngle;

            player.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);
        }

        private void MoveCamera()
        {
            if (Input.GetKeyDown(AccelerationButtton))
                speed *= accelerationRatio;

            if (Input.GetKeyUp(AccelerationButtton))
                speed /= accelerationRatio;

            if (Input.GetKey(moveForwardButton))
                player.Translate(Vector3.forward * speed * Time.deltaTime);
            if (Input.GetKey(moveLeftButton))
                player.Translate(Vector3.right * speed * Time.deltaTime * (-1));
            if (Input.GetKey(moveBackButton))
                player.Translate(Vector3.forward * speed * Time.deltaTime * (-1));
            if (Input.GetKey(moveRightButton))
                player.Translate(Vector3.right * speed * Time.deltaTime);
        }

        public float GetLocalizationDelay()
        {
            return settings.localizationTimeout;
        }

        public void Run()
        {
            StartCoroutine(LocalizeWithDelay(GetLocalizationDelay()));
        }

        public void Stop()
        {
            StopAllCoroutines();
            localisationService?.StopAllCoroutines();
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        public LocationState GetLocationRequest()
        {
            LocationState locationState = new LocationState();
            locationState.Status = LocalisationStatus.VPS_READY;

            LocalisationResult localisationResult = new LocalisationResult();
            localisationResult.LocationId = provider.GetTracking().GetLocalTracking().LocationId;
            localisationResult.VpsPosition = provider.GetTracking().GetLocalTracking().Position;
            localisationResult.VpsRotation = provider.GetTracking().GetLocalTracking().Rotation.eulerAngles;

            locationState.Localisation = localisationResult;

            return locationState;
        }

        private IEnumerator LocalizeWithDelay(float delay)
        {
            while (true)
            {
                while (isPaused)
                    yield return null;

                do
                {
                    if (isCorrectAngle != CheckTakePhotoConditions(provider.GetTracking().GetLocalTracking().Rotation.eulerAngles, settings))
                    {
                        isCorrectAngle = !isCorrectAngle;
                        OnCorrectAngle?.Invoke(isCorrectAngle);
                    }
                    if (!isCorrectAngle)
                        yield return null;
                } while (!isCorrectAngle);

                yield return new WaitForSeconds(delay);
                LocationState locationState = GetLocationRequest();
                provider.GetTracking().Localize(locationState.Localisation.LocationId);
                OnLocalisationHappend?.Invoke(locationState);
            }
        }

        public void SetSettings(SettingsVPS settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Localize camera in mockPosition and mockRotation
        /// </summary>
        /// <param name="mockPosition"></param>
        /// <param name="mockRotation"></param>
        private void MockLocalize(string locationId, Vector3 mockPosition, Quaternion mockRotation)
        {
            MockLocalize(locationId, mockPosition, mockRotation.eulerAngles);
        }

        /// <summary>
        /// Localize camera in mockPosition and mockRotation
        /// </summary>
        /// <param name="mockPosition"></param>
        /// <param name="mockRotation"></param>
        private void MockLocalize(string locationId, Vector3 mockPosition, Vector3 mockRotation)
        {
            LocalisationResult localisationResult = new LocalisationResult();
            localisationResult.VpsPosition = mockPosition;
            localisationResult.VpsRotation = mockRotation;
            localisationResult.TrackingPosition = provider.GetTracking().GetLocalTracking().Position;
            localisationResult.TrackingRotation = provider.GetTracking().GetLocalTracking().Rotation.eulerAngles;
            localisationResult.LocationId = locationId;

            LocationState locationState = new LocationState();
            locationState.Status = LocalisationStatus.VPS_READY;
            locationState.Localisation = localisationResult;

            provider.GetARFoundationApplyer().ApplyVPSTransform(localisationResult, true);
            provider.GetTracking().Localize(locationId);

            OnLocalisationHappend?.Invoke(locationState);
        }

        public bool CheckTakePhotoConditions(Vector3 curAngle, SettingsVPS settings)
        {
            return (curAngle.x < settings.MaxAngleX || curAngle.x > 360 - settings.MaxAngleX) &&
            (curAngle.z < settings.MaxAngleZ || curAngle.z > 360 - settings.MaxAngleZ);
        }
    }
}
