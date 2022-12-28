using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace naviar.VPSService
{
    public class DownloadNeuronStatus
    {
        private const string bucketPath = "https://mobile-weights.naviar.io/";

        public string Name;
        public string Url;
        public string DataPath;
        public float Progress;

        public DownloadNeuronStatus(string name, bool downloadFromCloud)
        {
            Name = name;
            Url = Path.Combine(bucketPath, name).Replace("\\", "/");
            DataPath = downloadFromCloud ? Path.Combine(Application.persistentDataPath, name) : Path.Combine(Application.streamingAssetsPath, name);
            Progress = 0f;
        }
    }

    public class VPSPrepareStatus
    {
        private bool downloadFromCloud = false;

        private DownloadNeuronStatus imageEncoder;
        private DownloadNeuronStatus imageFeatureExtractor;

        public event System.Action OnVPSReady;

        #region Metrics

        private const string DownloadMVPSTime = "DownloadMVPSTime";

        #endregion

        public VPSPrepareStatus()
        {
            imageEncoder = new DownloadNeuronStatus(MobileVPS.ImageEncoderFileName, downloadFromCloud);
            imageFeatureExtractor = new DownloadNeuronStatus(MobileVPS.ImageFeatureExtractorFileName, downloadFromCloud);

            // if mobileVPS already ready
            if (IsReady())
            {
                imageEncoder.Progress = 1;
                imageFeatureExtractor.Progress = 1;
            }
        }

        /// <summary>
        /// Download all mobileVPS neurals
        /// </summary>
        public IEnumerator DownloadNeurals()
        {
            CheckDirectory(Directory.GetParent(imageEncoder.DataPath).FullName);
            while (!File.Exists(imageEncoder.DataPath) || !File.Exists(imageFeatureExtractor.DataPath))
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    VPSLogger.Log(LogLevel.ERROR, "No internet to download MobileVPS");
                }
                yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);
                VPSLogger.Log(LogLevel.DEBUG, "Start downloading MobileVPS");

                yield return DownloadNeural(imageFeatureExtractor);
                yield return DownloadNeural(imageEncoder);
            }

            VPSLogger.Log(LogLevel.DEBUG, "Mobile vps network downloaded successfully!");
            OnVPSReady?.Invoke();
        }

        /// <summary>
        /// Download mobileVPS neural
        /// </summary>
        private IEnumerator DownloadNeural(DownloadNeuronStatus neuron)
        {
            if (File.Exists(neuron.DataPath))
            {
                neuron.Progress = 1;
                yield break;
            }

            while (true)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    VPSLogger.Log(LogLevel.ERROR, "No internet to download MobileVPS");
                }
                yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);

                MetricsCollector.Instance.StartStopwatch(DownloadMVPSTime);

                using (UnityWebRequest www = UnityWebRequest.Get(neuron.Url))
                {
                    www.SendWebRequest();
                    while (!www.isDone)
                    {
                        neuron.Progress = www.downloadProgress;
                        VPSLogger.LogFormat(LogLevel.DEBUG, "Current progress: {0}", neuron.Progress);
                        yield return null;
                    }

                    // check error
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        VPSLogger.LogFormat(LogLevel.ERROR, "Can't download mobile vps network: {0}", www.error);
                        yield return null;
                        continue;
                    }

                    neuron.Progress = www.downloadProgress;
                    File.WriteAllBytes(neuron.DataPath, www.downloadHandler.data);
                    VPSLogger.Log(LogLevel.DEBUG, "Mobile vps network downloaded successfully!");
                    OnVPSReady?.Invoke();

                    MetricsCollector.Instance.StopStopwatch(DownloadMVPSTime);

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0}{1} {2}", DownloadMVPSTime, neuron.Name, MetricsCollector.Instance.GetStopwatchSecondsAsString(DownloadMVPSTime));

                    break;
                }
            }
        }

        /// <summary>
        /// Get download progress (between 0 and 1)
        /// </summary>
        public float GetProgress()
        {
            return (imageEncoder.Progress + imageFeatureExtractor.Progress) / 2f;
        }

        /// <summary>
        /// Is mobileVPS ready?
        /// </summary>
        private bool IsReady()
        {
            return !downloadFromCloud || File.Exists(imageEncoder.DataPath) && File.Exists(imageFeatureExtractor.DataPath);
        }

        private void CheckDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
