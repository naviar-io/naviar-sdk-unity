using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public enum LocalisationStatus { NO_LOCALISATION, GPS_ONLY, VPS_READY }

    /// <summary>
    /// Last localization request data
    /// </summary>
    public class LocationState
    {
        public LocalisationStatus Status;
        public ErrorInfo Error;
        public LocalisationResult Localisation;

        public LocationState()
        {
            Status = LocalisationStatus.NO_LOCALISATION;
            Error = null;
            Localisation = new LocalisationResult();
        }
    }
}
