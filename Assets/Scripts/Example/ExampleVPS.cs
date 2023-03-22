using System.Collections;
using System.Collections.Generic;
using naviar.VPSService;
using UnityEngine;

public class ExampleVPS : MonoBehaviour
{
	/// <summary>
    /// VPS API class to control VPS Service
    /// </summary>
    public VPSLocalisationService VPS;

    private IEnumerator Start()
    {
		// Waiting for Mobile Vps to load 
		yield return new WaitUntil(() => VPS.IsReady());

		// Subscribe to success and error vps result
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;

		// Create custom settings to VPS: generate url in constructor and set delay between requests
		SettingsVPS settings = new SettingsVPS(new string[] { "polytech" }, 5);

		settings.localizationTimeout = 3;

		// Start service
		VPS.StartVPS(settings);
	}
	
	/// <summary>
    /// Request finished successfully
    /// </summary>
    /// <param name="locationState"></param> 
	private void OnPositionUpdatedHandler(LocationState locationState)
	{
		Debug.LogFormat("[Event] Localisation successful! Receive position {0} and rotation {1}", locationState.Localisation.VpsPosition, locationState.Localisation.VpsRotation);
	}

	/// <summary>
	/// Request finished with error
	/// </summary>
	/// <param name="errorCode"></param>
	private void OnErrorHappendHandler(ErrorInfo errorCode)
    {
		Debug.LogFormat("[Event] Localisation error: {0}", errorCode.LogDescription());
	}

	/// <summary>
    /// In the end stop vps service
    /// </summary>
	private void OnDestroy()
	{
		VPS.StopVps();
	}
}