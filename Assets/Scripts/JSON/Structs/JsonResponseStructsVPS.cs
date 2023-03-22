using System;
using Newtonsoft.Json;

namespace naviar.VPSService.JSONs
{
    [Serializable]
    public class ResponseStruct
    {
        public ResponseData data;
    }

    [Serializable]
    public class ResponseData
    {
        public string status;
        [JsonProperty("status_description")]
        public string statusDescription;
        public ResponseAttributes attributes;
    }

    [Serializable]
    public class ResponseAttributes
    {
        [JsonProperty("location_id")]
        public string locationId;
        public ResponseLocation location;
        [JsonProperty("tracking_pose")]
        public TrackingPose trackingPose;
        [JsonProperty("vps_pose")]
        public TrackingPose vpsPose;
    }

    [Serializable]
    public class ResponseLocation
    {
        public RequstGps gps;
        public RequestCompass compass;
    }

    [Serializable]
    public class FailDetails
    {
        public FailDetail[] detail;
    }

    [Serializable]
    public class FailDetail
    {
        public string[] loc;
        public string msg;
        public string type;
    }

    [Serializable]
    public class FailStringDetail
    {
        public string detail;
    }
}
