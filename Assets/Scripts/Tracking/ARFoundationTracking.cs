using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace naviar.VPSService
{
    public class ARFoundationTracking : MonoBehaviour, ITracking
    {
        private Transform ARCamera;
        private TrackingData trackingData = new TrackingData();

        private void Awake()
        {
            ARCamera = FindObjectOfType<XROrigin>().Camera.transform;
            if (ARCamera == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available for tracking");
            }
        }

        /// <summary>
        /// Write current position and rotation from camera in the structure
        /// </summary>
        private void UpdateTrackingData()
        {
            if (ARCamera != null)
            {
                trackingData.Position = ARCamera.localPosition;
                trackingData.Rotation = ARCamera.localRotation;
            }
        }

        public TrackingData GetLocalTracking()
        {
            UpdateTrackingData();
            return trackingData;
        }

        public bool Localize(string locationId)
        {
            if (trackingData.LocationId != locationId)
            {
                trackingData.LocationId = locationId;
                return true;
            }
            return false;
        }

        public void ResetTracking()
        {
            trackingData = new TrackingData();
        }

        public bool IsLocalized()
        {
            return !string.IsNullOrEmpty(trackingData.LocationId);
        }
    }
}
