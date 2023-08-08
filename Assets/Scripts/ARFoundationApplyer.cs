using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace naviar.VPSService
{
    public class ARFoundationApplyer : MonoBehaviour
    {
        private XROrigin xrOrigin;

        [Tooltip("Max distance for interpolation")]
        public float MaxInterpolationDistance = 5;

        [Tooltip("Interpolation speed")]
        public float LerpSpeed = 2.0f;

        [Tooltip("Override only North direction or entire phone rotation")]
        public bool RotateOnlyY = true;

        [Tooltip("Freeze Y position")]
        public bool FreezeYPos = false;

        private void Start()
        {
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "ARSessionOrigin is not found");
            }
        }

        /// <summary>
        /// Apply taked transform and return adjusted ARFoundation localisation
        /// </summary>
        /// <returns>The VPS Transform.</returns>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation, bool instantly = false)
        {
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization position: {0}", localisation.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization rotation: {0}", localisation.VpsRotation);
            LocalisationResult correctedResult = (LocalisationResult)localisation.Clone();

            if (FreezeYPos)
                correctedResult.VpsPosition.y = 0;

            // calculate camera offset for the time of sending request
            Vector3 cameraOffset = xrOrigin.Camera.transform.localPosition - localisation.TrackingPosition;

            // subtract the sent position and rotation because the child has them
            correctedResult.VpsPosition -= correctedResult.TrackingPosition;
            correctedResult.VpsRotation -= correctedResult.TrackingRotation;

            StopAllCoroutines();
            StartCoroutine(UpdatePosAndRot(correctedResult.VpsPosition, correctedResult.VpsRotation, cameraOffset, instantly));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization position: {0}", correctedResult.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization rotation: {0}", correctedResult.VpsRotation);

            return correctedResult;
        }

        /// <summary>
        /// Apply NewPosition and NewRotationY with interpolation
        /// </summary>
        /// <returns>The position and rotation.</returns>
        /// <param name="NewPosition">New position.</param>
        /// <param name="NewRotation">New rotation y.</param>
        IEnumerator UpdatePosAndRot(Vector3 NewPosition, Vector3 NewRotation, Vector3 cameraOffset, bool instantly)
        {
            if (RotateOnlyY)
            {
                NewRotation.x = 0;
                NewRotation.z = 0;
            }

            // save current anchor position and rotation
            Vector3 startPosition = xrOrigin.transform.position;
            Quaternion startRotation = xrOrigin.transform.rotation;

            // set new position
            xrOrigin.transform.position = NewPosition;
            // we need rotate only camera, so we reset parent rotation
            xrOrigin.transform.rotation = Quaternion.identity;
            // calculate camera world position without offset
            Vector3 cameraPosWithoutOffet = xrOrigin.Camera.transform.position - cameraOffset;
            // and rotate parent around child on three axes
            RotateAroundThreeAxes(NewRotation, cameraPosWithoutOffet);

            // save anchor position and rotation
            Vector3 targetPosition = xrOrigin.transform.position;
            Quaternion targetRotation = xrOrigin.transform.rotation;

            // if the offset is greater than MaxInterpolationDistance - don't use interpolation (move instantly)
            if (Vector3.Distance(startPosition, targetPosition) > MaxInterpolationDistance || instantly)
                yield break;

            // interpolate position and rotation from start pos to target
            float interpolant = 0;
            while (interpolant < 1)
            {
                interpolant += LerpSpeed * Time.deltaTime;
                xrOrigin.transform.position = Vector3.Lerp(startPosition, targetPosition, interpolant);
                xrOrigin.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, interpolant);
                yield return null;
            }
        }

        private void RotateAroundThreeAxes(Vector3 rotateVector, Vector3 cameraPosWithoutOffet)
        {
            // rotate anchor (parent) around camera (child)
            xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.forward, rotateVector.z);
            xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.right, rotateVector.x);
            xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.up, rotateVector.y);
        }

        public void ResetTracking()
        {
            StopAllCoroutines();
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = Vector3.zero;
                xrOrigin.transform.rotation = Quaternion.identity;
                xrOrigin.Camera.transform.position = Vector3.zero;
                xrOrigin.Camera.transform.rotation = Quaternion.identity;
            }
        }
    }
}