using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace naviar.VPSService
{
    public class TrackingData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsLocalisedLocation;
        public string LocationId;
    }
}