using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] AudioSettings audioSettings;
    [SerializeField] InputSettings inputSettings;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenPauseMenu();
        }
    }
    public void StoreValues() 
    {
        StaticData.MasterVolume = audioSettings.GetMasterVolume();
        StaticData.MusicVolume = audioSettings.GetMusicVolume();
        StaticData.AmbienceVolume = audioSettings.GetAmbienceVolume();
        StaticData.EffectsVolume = audioSettings.GetEffectsVolume();
        StaticData.VoicesVolume = audioSettings.GetVoicesVolume();

    }

    public void OpenPauseMenu() 
    {
        menuPanel.GetComponent<MainMenuScript>().Pause();
    }

}
