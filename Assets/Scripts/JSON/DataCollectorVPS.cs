using System;
using naviar.VPSService;
using UnityEngine;
using Newtonsoft.Json;

namespace naviar.VPSService.JSONs
{
    /// <summary>
    /// Serialization and deserealisation JSON 
    /// </summary>
    public static class DataCollector
    {
        /// <summary>
        /// Create request structure from providers data
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="Provider">Provider.</param>
        public static RequestStruct CollectData(ServiceProvider Provider, string[] locationIds)
        {
            Pose pose = new Pose();
            var tracking = Provider.GetTracking().GetLocalTracking();

            pose.position = tracking.Position;
            pose.rotation = tracking.Rotation;

            IServiceGPS gps = Provider.GetGPS();
            RequestLocation requestLocation = null;
            if (gps != null)
            {
                GPSData gpsData = gps.GetGPSData();
                CompassData gpsCompass = gps.GetCompassData();

                if (gpsData.Accuracy < 1000)
                {
                    RequstGps requstGps = new RequstGps
                    {
                        latitude = gpsData.Latitude,
                        longitude = gpsData.Longitude,
                        altitude = gpsData.Altitude,
                        accuracy = gpsData.Accuracy,
                        timestamp = gpsData.Timestamp
                    };
                    RequestCompass requestCompass = new RequestCompass
                    {
                        heading = gpsCompass.Heading,
                        accuracy = gpsCompass.Accuracy,
                        timestamp = gpsCompass.Timestamp
                    };

                    requestLocation = new RequestLocation()
                    {
                        gps = requstGps,
                        compass = requestCompass
                    };
                }
            }

            Vector2 FocalPixelLength = Provider.GetCamera().GetFocalPixelLength();
            Vector2 PrincipalPoint = Provider.GetCamera().GetPrincipalPoint();

            const string userIdKey = "user_id";
            if (!PlayerPrefs.HasKey(userIdKey))
            {
                PlayerPrefs.SetString(userIdKey, Guid.NewGuid().ToString());
            }

            var attrib = new RequestAttributes
            {
                locationIds = locationIds,
                sessionId = Provider.GetSessionId(),
                userId = PlayerPrefs.GetString(userIdKey),
                timestamp = new DateTimeOffset(DateTime.Now).ToUniversalTime().ToUnixTimeMilliseconds() / 1000d,

                location = requestLocation,

                clientCoordinateSystem = "unity",

                trackingPose = new TrackingPose
                {
                    x = pose.position.x,
                    y = pose.position.y,
                    z = pose.position.z,
                    rx = pose.rotation.eulerAngles.x,
                    ry = pose.rotation.eulerAngles.y,
                    rz = pose.rotation.eulerAngles.z
                },

                intrinsics = new Intrinsics
                {
                    width = Provider.GetTextureRequirement().Width,
                    height = Provider.GetTextureRequirement().Height,

                    fx = FocalPixelLength.x,
                    fy = FocalPixelLength.y,
                    cx = PrincipalPoint.x,
                    cy = PrincipalPoint.y
                }
            };


            var data = new RequestData
            {
                attributes = attrib
            };

            var communicationStruct = new RequestStruct
            {
                data = data
            };

            return communicationStruct;
        }

        /// <summary>
        /// Serialize request to json
        /// </summary>
        public static string Serialize(RequestStruct meta)
        {
            var json = JsonConvert.SerializeObject(meta);

            VPSLogger.LogFormat(LogLevel.DEBUG, "Json to send: {0}", json);
            return json;
        }

        /// <summary>
        /// Deserialize server responce
        /// </summary>
        /// <returns>The deserialize.</returns>
        /// <param name="json">Json.</param>
        public static LocationState Deserialize(string json, long resultCode = 200)
        {
            LocationState request = new LocationState();
            switch(resultCode)
            {
                case 200:
                    {
                        ResponseStruct communicationStruct = JsonConvert.DeserializeObject<ResponseStruct>(json);
                        request.Status = GetStatusFromString(communicationStruct.data.status);

                        if (request.Status == LocalisationStatus.VPS_READY)
                        {
                            request.Error = null;
                            request.Localisation = new LocalisationResult
                            {
                                VpsPosition = new Vector3(communicationStruct.data.attributes.vpsPose.x,
                                            communicationStruct.data.attributes.vpsPose.y,
                                            communicationStruct.data.attributes.vpsPose.z),
                                VpsRotation = new Vector3(communicationStruct.data.attributes.vpsPose.rx,
                                            communicationStruct.data.attributes.vpsPose.ry,
                                            communicationStruct.data.attributes.vpsPose.rz),
                                TrackingPosition = new Vector3(communicationStruct.data.attributes.trackingPose.x,
                                            communicationStruct.data.attributes.trackingPose.y,
                                            communicationStruct.data.attributes.trackingPose.z),
                                TrackingRotation = new Vector3(communicationStruct.data.attributes.trackingPose.rx,
                                            communicationStruct.data.attributes.trackingPose.ry,
                                            communicationStruct.data.attributes.trackingPose.rz),
                                LocalitonId = communicationStruct.data.attributes.locationId
                            };
                        }
                        else
                        {
                            request.Localisation = null;
                            request.Error = new ErrorInfo(ErrorCode.LOCALISATION_FAIL, communicationStruct.data.statusDescription);
                        }
                        break;
                    }
                case 422:
                    {
                        FailDetails failDetail = JsonConvert.DeserializeObject<FailDetails>(json);
                        request.Localisation = null;

                        string errorField = "";
                        for (int i = 0; i < failDetail.detail[0].loc.Length; i++)
                        {
                            errorField += failDetail.detail[0].loc[i];
                            if (i != failDetail.detail[0].loc.Length - 1)
                                errorField += "/";
                        }
                        request.Error = new ErrorInfo()
                        {
                            Code = ErrorCode.VALIDATION_ERROR,
                            Message = failDetail.detail[0].msg,
                            JsonErrorField = errorField
                        };
                        break;
                    }
                case 500:
                case 404:
                    {
                        FailStringDetail failDetail = JsonConvert.DeserializeObject<FailStringDetail>(json);
                        request.Localisation = null;

                        request.Error = new ErrorInfo()
                        {
                            Code = ErrorCode.SERVER_INTERNAL_ERROR,
                            Message = failDetail.detail
                        };
                    }
                    break;
            }

            return request;
        }

        static LocalisationStatus GetStatusFromString(string status)
        {
            switch(status)
            {
                case "done": return LocalisationStatus.VPS_READY;
                case "fail": return LocalisationStatus.GPS_ONLY;
                default: return LocalisationStatus.GPS_ONLY;
            }
        }
    }
}