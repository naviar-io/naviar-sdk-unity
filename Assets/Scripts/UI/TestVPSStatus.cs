using System.Collections;
using System.Collections.Generic;
using naviar.VPSService;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Show VPS request status in UI
/// </summary>
public class TestVPSStatus : MonoBehaviour
{
	private VPSLocalisationService VPS;
	public Image imageStatus;

    private void OnEnable()
    {
		StartCoroutine(OnEnableRoutine());
	}

	private IEnumerator OnEnableRoutine()
    {
		VPS = FindObjectOfType<VPSLocalisationService>();
		if (!VPS)
			yield break;

		// Waiting for Mobile Vps to load 
		yield return new WaitUntil(() => VPS.IsReady());

		// Subscribe to success and error vps result
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;
	}

    private void OnDisable()
    {
		StopAllCoroutines();
		VPS.OnPositionUpdated -= OnPositionUpdatedHandler;
		VPS.OnErrorHappend -= OnErrorHappendHandler;
	}

    private void OnPositionUpdatedHandler(LocationState locationState)
	{
		imageStatus.color = Color.green;
		StartCoroutine(resetImageStatus());
	}

	private void OnErrorHappendHandler(ErrorInfo error)
	{
		imageStatus.color = Color.red;
		StartCoroutine(resetImageStatus());
	}

	public IEnumerator resetImageStatus()
	{
		yield return new WaitForSeconds(1);
		imageStatus.color = Color.white;
	}
}
