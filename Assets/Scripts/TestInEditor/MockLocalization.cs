using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockLocalization : MonoBehaviour
{
    public string locationId;
    public Vector3 position
    {
        get
        {
            return transform.position;
        }
    }
    public Quaternion rotation
    {
        get
        {
            return transform.rotation;
        }
    }
}
