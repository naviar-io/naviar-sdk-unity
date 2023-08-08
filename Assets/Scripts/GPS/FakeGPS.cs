using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    /// <summary>
    /// Return fake gps data 
    /// </summary>
    public class FakeGPS : MonoBehaviour, IServiceGPS
    {
        public float Latitude = 54.875f;
        public float Longitude = 48.6543f;
        public float Altitude = 72.4563f;
        public float GpsAccuracy = 0.5f;

        public float Heading = 55.33f;
        public float CompassAccuracy  = 0.4f;

        private new bool enabled = true;

        /// <summary>
        /// Create fake gps data
        /// </summary>
        private GPSData GenerateGPSData()
        {
            var gpsData = new GPSData();
            if (!enabled)
            {
                return gpsData;
            }

            gpsData.status = GPSStatus.Running;
            gpsData.Latitude = Latitude;
            gpsData.Longitude = Longitude;
            gpsData.Altitude = Altitude;
            gpsData.Accuracy = GpsAccuracy;
            gpsData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return gpsData;
        }

        /// <summary>
        /// Create fake compass data
        /// </summary>
        private CompassData GenerateCompassData()
        {
            var compassData = new CompassData();
            if (!enabled)
            {
                return compassData;
            }

            compassData.status = GPSStatus.Running;
            compassData.Heading = Heading;
            compassData.Accuracy = CompassAccuracy;
            compassData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return compassData;
        }

        public CompassData GetCompassData()
        {
            return GenerateCompassData();
        }

        public GPSData GetGPSData()
        {
            return GenerateGPSData();
        }

        public void SetEnable(bool enable)
        {
            enabled = enable;
        }
    }
}