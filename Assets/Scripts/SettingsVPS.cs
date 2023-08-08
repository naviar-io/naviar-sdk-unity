using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public class SettingsVPS
    {
        // The list of locations ids for localization
        public string[] locationIds;
        // Localization delay between sending
        public float localizationTimeout = 1;
        // Calibration delay between sending
        public float calibrationTimeout = 2.5f;
        // Fails count to reset session VPS
        public int failsCountToResetSession;

        public float MaxAngleX = 30;
        public float MaxAngleZ = 30;

        public SettingsVPS(string[] locationIds, int failsCountToResetSession)
        {
            this.locationIds = locationIds;
            this.failsCountToResetSession = failsCountToResetSession;
        }
    }
}