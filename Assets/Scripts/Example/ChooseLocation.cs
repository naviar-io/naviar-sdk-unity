using System.Collections;
using System.Collections.Generic;
using naviar.VPSService;
using UnityEngine;

public enum Location { POLYTECH, VDNH_ARCH, VDNH_PAVILION, ARTPLAY, FLACON, GORKYPARK_ARCH, KHLEBZAVOD }

public class ChooseLocation : MonoBehaviour
{
    public Location CurrentLocation;
    private VPSLocalisationService VPS;
    public GameObject[] Content;
    public GameObject[] Occluders;

    private void Start()
    {
        VPS = FindObjectOfType<VPSLocalisationService>();
        if (VPS == null)
        {
            Debug.LogError("VPS was not found on the scene");
            return;
        }

        VPS.locationsIds = GetLocationId(CurrentLocation);
        VPS.StartVPS();

        Content[(int)CurrentLocation].SetActive(false);
        VPS.OnPositionUpdated += (ls) => Content[(int)CurrentLocation].SetActive(true);

        var settingsToggles = FindObjectOfType<SettingsToggles>();
        if (settingsToggles != null)
            settingsToggles.OccluderModel = Occluders[(int)CurrentLocation];
    }

    public string[] GetLocationId(Location location)
    {
        switch(location)
        {
            case Location.POLYTECH:
                return new string[] { "polytech" };
            case Location.VDNH_ARCH:
                return new string[] { "vdnh_arka" };
            case Location.VDNH_PAVILION:
                return new string[] { "vdnh_pavilion" };
            case Location.ARTPLAY:
                return new string[] { "artplay" };
            case Location.FLACON:
                return new string[] { "flacon" };
            case Location.GORKYPARK_ARCH:
                return new string[] { "gorky_park" };
            case Location.KHLEBZAVOD:
                return new string[] { "hlebozavod9" };
        }

        return null;
    }
}
