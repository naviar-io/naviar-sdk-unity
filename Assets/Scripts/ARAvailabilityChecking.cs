using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace naviar.VPSService
{
    /// <summary>
    /// Checking for ar availability on the device
    /// </summary>
    public static class ARAvailabilityChecking
    {
        public static IEnumerator StartChecking(System.Action<bool> OnStatusReceived)
        {
            while (true)
            {
                switch (ARSession.state)
                {
                    case ARSessionState.None:
                    case ARSessionState.CheckingAvailability:
                        yield return ARSession.CheckAvailability();
                        continue;
                    case ARSessionState.NeedsInstall:
                        yield return ARSession.Install();
                        continue;
                    case ARSessionState.Unsupported:
                        OnStatusReceived?.Invoke(false);
                        yield break;
                    case ARSessionState.Ready:
                    case ARSessionState.SessionTracking:
                        OnStatusReceived?.Invoke(true);
                        yield break;
                    case ARSessionState.Installing:
                    case ARSessionState.SessionInitializing:
                        yield return null;
                        continue;
                }
            }
        }
    }
}