using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    /// <summary>
    /// Reset tracking by pressing a button
    /// </summary>
    public class ManualControl : MonoBehaviour
    {
        public VPSLocalisationService VPS;
        public KeyCode StartVPSKeyCode;
        public KeyCode ResetKeyCode;

        private void Update()
        {
            if (Input.GetKeyDown(StartVPSKeyCode))
                VPS.StartVPS();
            if (Input.GetKeyDown(ResetKeyCode))
                VPS.ResetTracking();
        }
    }
}
