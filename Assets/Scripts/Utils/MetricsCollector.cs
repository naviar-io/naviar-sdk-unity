using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    /// <summary>
    /// Class to collect time metrics
    /// </summary>
    public class MetricsCollector : MonoBehaviour
    {
        private static MetricsCollector instance = new MetricsCollector();
        public static MetricsCollector Instance
        {
            get
            {
                return instance;
            }
        }

        private Dictionary<string, System.Diagnostics.Stopwatch> stopwatches = new Dictionary<string, System.Diagnostics.Stopwatch>();

        public void StartStopwatch(string key)
        {
            if (!stopwatches.ContainsKey(key))
                stopwatches.Add(key, new System.Diagnostics.Stopwatch());
            try
            {
                stopwatches[key].Restart();
            }
            catch
            {

            }
        }

        public TimeSpan GetStopwatchTimespan(string key)
        {
            return stopwatches[key].Elapsed;
        }

        public void StopStopwatch(string key)
        {
            try
            {
                stopwatches[key].Stop();
            }
            catch
            {

            }
        }

        public string GetStopwatchSecondsAsString(string key)
        {
            TimeSpan timeSpan = stopwatches[key].Elapsed;
            return string.Format("{0:N10}", timeSpan.TotalSeconds);
        }
    }
}
