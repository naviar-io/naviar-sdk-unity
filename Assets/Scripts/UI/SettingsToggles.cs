using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace naviar.VPSService
{
    /// <summary>
    /// UI Canvas to change settings at runtime
    /// </summary>
    public class SettingsToggles : MonoBehaviour
    {
        public GameObject Root;
        public Toggle Autofocus;
        public Toggle SendGPS;
        public Toggle Occluder;
        public Toggle SaveImages;
        public Toggle WriteLogsInFile;

        public Button RestartVPSButton;
        public float PressTime = 2f;
        private float mouseDeltaTime = 0;

        public GameObject OccluderModel;
        public Material VisibleMaterial;
        public Material OccluderMaterial;

        private VPSLocalisationService vps;
        private VPSLocalisationService VPS
        {
            get
            {
                if (vps == null)
                    vps = FindObjectOfType<VPSLocalisationService>();
                return vps;
            }
        }

        private ARCameraManager cameraManager;
        public ARCameraManager CameraManager
        {
            get
            {
                if (cameraManager == null)
                    cameraManager = FindObjectOfType<ARCameraManager>();
                return cameraManager;
            }
        }

        private void Awake()
        {
            Autofocus?.onValueChanged.AddListener((value) => CameraManager.autoFocusRequested = value);
            SendGPS?.onValueChanged.AddListener((value) => VPS.SendGPS = value);
            Occluder?.onValueChanged.AddListener((value) => ApplyOccluder(value));
            SaveImages.onValueChanged.AddListener((value) => OnSaveImages(value));
            WriteLogsInFile.onValueChanged.AddListener((value) => VPSLogger.WriteLogsInFile = value);

            RestartVPSButton.onClick.AddListener(() =>
            {
                VPS.ResetTracking();
                VPS.StartVPS();
                HideToggles();
            });

            HideToggles();
        }

        private void OnSaveImages(bool saveImages)
        {
            DebugUtils.SaveImagesLocaly = saveImages;
        }

        private void Start()
        {
            if (Autofocus != null)
                Autofocus.isOn = CameraManager.autoFocusRequested;
            if (SendGPS != null)
                SendGPS.isOn = VPS.SendGPS;
            if (Occluder != null)
                Occluder.isOn = false;
            if (SaveImages != null)
                SaveImages.isOn = DebugUtils.SaveImagesLocaly;
            if (WriteLogsInFile != null)
                WriteLogsInFile.isOn = VPSLogger.WriteLogsInFile;

            ApplyOccluder(false);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                mouseDeltaTime += Time.deltaTime;
                if (mouseDeltaTime >= PressTime)
                {
                    ShowToggles();
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                mouseDeltaTime = 0f;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    mouseDeltaTime += Time.deltaTime;
                    if (mouseDeltaTime >= PressTime)
                    {
                        ShowToggles();
                    }
                }
                else
                {
                    mouseDeltaTime = 0f;
                }
            }
#endif
        }

        private void ShowToggles()
        {
            Root.gameObject.SetActive(true);
        }

        private void HideToggles()
        {
            Root.gameObject.SetActive(false);
        }

        private void ApplyOccluder(bool enable)
        {
            Renderer[] renderers = OccluderModel.GetComponentsInChildren<Renderer>();
            foreach(Renderer renderer in renderers)
            {
                if (enable)
                    renderer.material = VisibleMaterial;
                else
                    renderer.material = OccluderMaterial;
            }
        }
    }
}
