using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using naviar.VPSService.JSONs;
using UnityEngine;

namespace naviar.VPSService
{
    /// <summary>
    /// Requst to VPS server
    /// </summary>
    public class HttpClientRequestVPS : IRequestVPS
    {
        private string serverUrl;
        // api for localisation
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

            MultipartFormDataContent form = new MultipartFormDataContent();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            HttpContent img = new ByteArrayContent(binaryImage);
            form.Add(img, "image", CreateFileName());

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            MetricsCollector.Instance.StartStopwatch(ImageVPSRequest);

            yield return Task.Run(() => SendRequest(uri, form, timeout)).AsCoroutine();

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

            MultipartFormDataContent form = new MultipartFormDataContent();

            HttpContent embd = new ByteArrayContent(embedding);
            form.Add(embd, "embedding", "data.embd");

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            MetricsCollector.Instance.StartStopwatch(MVPSRequest);

            yield return Task.Run(() => SendRequest(uri, form, timeout)).AsCoroutine();

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

            MultipartFormDataContent form = new MultipartFormDataContent();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            HttpContent img = new ByteArrayContent(binaryImage);
            form.Add(img, "image", CreateFileName());

            HttpContent embd = new ByteArrayContent(embedding);
            form.Add(embd, "embedding", "data.embd");

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            MetricsCollector.Instance.StartStopwatch(MVPSRequest);

            yield return Task.Run(() => SendRequest(uri, form, timeout)).AsCoroutine();

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

        private void SendRequest(string uri, MultipartFormDataContent form, int timeout)
        {
            using (var client = new HttpClient())
            {
                if (timeout != 0)
                    client.Timeout = TimeSpan.FromSeconds(timeout);

                var result = client.PostAsync(uri, form);

                System.Net.HttpStatusCode responseCode;
                try
                {
                    responseCode = result.Result.StatusCode;
                }
                catch
                {
                    ErrorInfo errorStruct = new ErrorInfo(ErrorCode.NO_INTERNET, "Network is not available");
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                    VPSLogger.LogFormat(LogLevel.ERROR, "Network error: {0}", errorStruct.LogDescription());
                    return;
                }

                VPSLogger.LogFormat(LogLevel.DEBUG, "Request finished with code: {0}", responseCode);
                
                List<string> requestIds = result.Result.Headers.GetValues("x-request-id").ToList();

                string xRequestId = "x-request-id: " + requestIds[0];

                for (int i = 1; i < requestIds.Count; i++)
                    xRequestId += ", " + requestIds[i];

                string response = result.Result.Content.ReadAsStringAsync().Result;

                if (responseCode == System.Net.HttpStatusCode.OK)
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Request Finished Successfully!\n{0}\n{1}", xRequestId, response);
                else
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Request Finished with error!\n{0}\n{1}", xRequestId, response);

                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(response, (long)responseCode);
                }
                catch (Exception e)
                {
                    VPSLogger.Log(LogLevel.ERROR, e);
                    ErrorInfo errorStruct = new ErrorInfo(ErrorCode.DESERIALIZED_ERROR, "Can't deserialize server response");
                    VPSLogger.Log(LogLevel.ERROR, errorStruct.LogDescription());
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                    return;
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
                    return;
                }
            }
        }
    }
}