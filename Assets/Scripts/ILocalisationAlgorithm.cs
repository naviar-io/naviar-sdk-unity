using naviar.VPSService;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILocalisationAlgorithm { 
    public void Run();
    public void Stop();
    public void Pause();
    public void Resume();
    LocationState GetLocationRequest();

    public event System.Action<LocationState> OnLocalisationHappend;
    public event System.Action<ErrorInfo> OnErrorHappend;
    public event System.Action<bool> OnCorrectAngle;
}
