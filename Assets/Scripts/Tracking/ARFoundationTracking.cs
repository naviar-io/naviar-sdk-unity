using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace naviar.VPSService
{
    public class ARFoundationTracking : MonoBehaviour, ITracking
    {
        private GameObject ARCamera;
        private TrackingData trackingData;

        private void Awake()
        {
            ARCamera = FindObjectOfType<ARSessionOrigin>().camera.gameObject;
            if (ARCamera == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available for tracking");
            }
            trackingData = new TrackingData();
        }

        /// <summary>
        /// Write current position and rotation from camera in the structure
        /// </summary>
        private void UpdateTrackingData()
        {
            if (ARCamera != null)
            {
                trackingData.Position = ARCamera.transform.localPosition;
                trackingData.Rotation = ARCamera.transform.localRotation;
            }
        }

        public TrackingData GetLocalTracking()
        {
            UpdateTrackingData();
            return trackingData;
        }

        public bool Localize(string locationId)
        {
            if (trackingData != null)
            {
                trackingData.IsLocalisedLocation = true;
                trackingData.LocationId = locationId;
                return false;
            }
            if (trackingData.LocationId != locationId)
            {
                trackingData.LocationId = locationId;
                return true;
            }

            return false;
        }

        public void ResetTracking()
        {
            if (trackingData != null)
            {
                trackingData.IsLocalisedLocation = false;
            }
        }
    }
}
