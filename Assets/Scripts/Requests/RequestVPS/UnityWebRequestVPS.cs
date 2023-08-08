using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using naviar.VPSService.JSONs;
using UnityEngine;
using UnityEngine.Networking;

namespace naviar.VPSService
{
    /// <summary>
    /// Requst to VPS server
    /// </summary>
    public class UnityWebRequestVPS : IRequestVPS
    {
        private string serverUrl;
        // api for one photo localisation
        private string api_path_session = "vps/api/v3";

        private int timeout = 0;

        private LocationState locationState = new LocationState();

        #region Metrics

        private const string ImageVPSRequest = "ImageVPSRequest";
        private const string MVPSRequest = "MVPSRequest";

        #endregion

        public void SetUrl(string url)
        {
            serverUrl = url;
        }

        public IEnumerator SendVpsRequest(Texture2D image, string meta, System.Action callback)
        {
            string uri = Path.Combine(serverUrl, api_path_session).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            form.AddBinaryData("image", binaryImage, CreateFileName(), "image/jpeg");

            form.AddField("json", meta);

            MetricsCollector.Instance.StartStopwatch(ImageVPSRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(ImageVPSRequest);

            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", ImageVPSRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(ImageVPSRequest));

            callback();
        }

        public IEnumerator SendVpsRequest(byte[] embedding, string meta, System.Action callback)
        {
            string uri = Path.Combine(serverUrl, api_path_session).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            form.AddBinaryData("embedding", embedding, "data.embd");

            form.AddField("json", meta);

            MetricsCollector.Instance.StartStopwatch(MVPSRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(MVPSRequest);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", MVPSRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(MVPSRequest));

            callback();
        }

        public IEnumerator SendVpsRequest(Texture2D image, byte[] embedding, string meta, Action callback)
        {
            string uri = Path.Combine(serverUrl, api_path_session).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }

            form.AddBinaryData("image", binaryImage, CreateFileName(), "image/jpeg");
            form.AddBinaryData("embedding", embedding, "data.embd");
            form.AddField("json", meta);

            MetricsCollector.Instance.StartStopwatch(MVPSRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(MVPSRequest);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", MVPSRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(MVPSRequest));

            callback();
        }

        public LocationState GetLocationState()
        {
            return locationState;
        }

        /// <summary>
        /// Create name for image from current date and time
        /// </summary>
        private string CreateFileName()
        {
            DateTime dateTime = DateTime.Now;
            string file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
            file += ".jpg";
            return file;
        }

        /// <summary>
        /// Convert Texture2D to byte array
        /// </summary>
        private byte[] GetByteArrayFromImage(Texture2D image)
        {
            byte[] bytesOfImage = image.EncodeToJPG(100);
            return bytesOfImage;
        }

        /// <summary>
        /// Update latest response data
        /// </summary>
        /// <param name="Status">Status.</param>
        /// <param name="Error">Error.</param>
        /// <param name="Localisation">Localisation.</param>
        private void UpdateLocalisationState(LocalisationStatus Status, ErrorInfo Error, LocalisationResult Localisation)
        {
            locationState.Status = Status;
            locationState.Error = Error;
            locationState.Localisation = Localisation;
        }

        private IEnumerator SendRequest(string uri, WWWForm form)
        {
            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                www.downloadHandler = new DownloadHandlerBuffer();

                www.timeout = timeout;

                www.SendWebRequest();
                while (!www.isDone)
                {
                    yield return null;
                }

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    ErrorInfo errorStruct = new ErrorInfo(ErrorCode.NO_INTERNET, "Network is not available");
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                    VPSLogger.LogFormat(LogLevel.ERROR, "Network error: {0}", errorStruct.LogDescription());
                    yield break;
                }

                VPSLogger.LogFormat(LogLevel.DEBUG, "Request finished with code: {0}", www.responseCode);

                string xRequestId;
                var responseHeader = www.GetResponseHeaders();
                if (responseHeader == null)
                    xRequestId = "Response header is null";
                else
                    xRequestId = "x-request-id: " + responseHeader["x-request-id"];

                string response;
                var downloadHandler = www.downloadHandler;
                if (downloadHandler == null)
                    response = "Response download handler is null";
                else
                    response = downloadHandler.text;

                if (www.responseCode == 200)
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Request Finished Successfully!\n{0}\n{1}", xRequestId, response);
                else
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Request Finished with error!\n{0}\n{1}", xRequestId, response);

                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(response, www.responseCode);
                }
                catch
                {
                    ErrorInfo errorStruct = new ErrorInfo(ErrorCode.DESERIALIZED_ERROR, "Can't deserialize server response");
                    VPSLogger.Log(LogLevel.ERROR, errorStruct.LogDescription());
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                    yield break;
                }

                if (deserialized != null)
                {
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Server status {0}", deserialized.Status);
                    locationState = deserialized;
                }
                else
                {
                    ErrorInfo errorStruct = new ErrorInfo(ErrorCode.DESERIALIZED_ERROR, "There is no data come from server");
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                    VPSLogger.Log(LogLevel.ERROR, errorStruct.LogDescription());
                    yield break;
                }
            }
        }
    }
}