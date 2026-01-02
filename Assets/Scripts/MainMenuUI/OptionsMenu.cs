using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour // the methods for the options menu
{
    public AudioMixer audioMixer; // calls an audio mixer

    public Dropdown resDropdown; // calls a dropdown menu

    Resolution[] res; // set an array for the possible resolutions on a given PC

    void Start() // method to get the correct resolutions for one's PC and also set the default to your default
    {
        res = Screen.resolutions;
        resDropdown.ClearOptions(); // clears the default resolutions given by Unity

        List<string> resOptions = new List<string>(); // resolutions are integers but the options are strings, so they must be converted
        int currentResIndex = 0; // set the current resolution as 0 for use later

        for (int i = 0; i < res.Length; i++) // for the length of the resolution array
        {
            string option = res[i].width + "x" + res[i].height;
            resOptions.Add(option);

            if (res[i].width == Screen.currentResolution.width && res[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resDropdown.AddOptions(resOptions); // adds all the resolutions available on your PC
        resDropdown.value = currentResIndex;
        resDropdown.RefreshShownValue();
    }

    public void SetRes (int ResIndex) // set the resolution to the current one on your computer
    {
        Resolution resolution = res[ResIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    public void SetVolume (float volume)
    {
        audioMixer.SetFloat("volume", volume); // sets the volume
    }

    public void SetQuality(int qualIndex)
    {
        QualitySettings.SetQualityLevel(qualIndex); // sets the quality, currently unused
    }

    public void SetFullScreen (bool isFullScreen) // toggle that sets full screen
    {
        Screen.fullScreen = isFullScreen;
    }
}

