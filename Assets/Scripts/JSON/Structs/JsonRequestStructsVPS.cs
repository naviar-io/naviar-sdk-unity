using System;
using Newtonsoft.Json;
using UnityEngine;

namespace naviar.VPSService.JSONs
{
    [Serializable]
    public class RequestStruct
    {
        public RequestData data;
    }

    [Serializable]
    public class RequestData
    {
        public RequestAttributes attributes;
    }

    [Serializable]
    public class RequestAttributes
    {
        [JsonProperty("location_ids")]
        public string[] locationIds;
        [JsonProperty("session_id")]
        public string sessionId;
        [JsonProperty("user_id")]
        public string userId;
        public double timestamp;
        public RequestLocation location;
        [JsonProperty("client_coordinate_system")]
        public string clientCoordinateSystem;
        [JsonProperty("tracking_pose")]
        public TrackingPose trackingPose;
        public Intrinsics intrinsics;
    }

    [Serializable]
    public class RequestLocation
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RequstGps gps;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RequestCompass compass;
    }

    [Serializable]
    public class RequstGps
    {
        public double latitude;
        public double longitude;
        public double altitude;
        public double accuracy;
        public double timestamp;
    }

    [Serializable]
    public class TrackingPose
    {
        public float x;
        public float y;
        public float z;

        public float rx;
        public float ry;
        public float rz;
    }

    [Serializable]
    public class RequestCompass
    {
        public float heading;
        public float accuracy;
        public double timestamp;
    }

    [Serializable]
    public class Intrinsics
    {
        public int width;
        public int height;

        public float fx;
        public float fy;
        public float cx;
        public float cy;
    }
}
