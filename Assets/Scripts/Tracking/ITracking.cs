using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public interface ITracking
    {
        /// <summary>
        /// Get current tracking data;
        /// updates only on request
        /// </summary>
        /// <returns>The local tracking.</returns>
        TrackingData GetLocalTracking();
        /// <summary>
        /// Set localize flag in true
        /// Return true is location id was changed
        /// </summary>
        bool Localize(string locationId);
        /// <summary>
        /// Reset current tracking
        /// </summary>
        void ResetTracking();
    }
}
