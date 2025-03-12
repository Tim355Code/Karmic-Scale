using UnityEngine;
using UnityEngine.Audio;

// Code on the main menu UI, very simple and quick done using built-in UI components
public class Menu : MonoBehaviour
{
    public GameObject MainContainer;
    public GameObject CreditsContainer;
    public GameObject StartContainer;
    public GameObject TutorialContainer;

    public AudioMixer Mixer;
    private bool LoadingGame;

    public void SwitchToMain()
    {
        MainContainer.SetActive(true);
        CreditsContainer.SetActive(false);
        StartContainer.SetActive(false);
        TutorialContainer.SetActive(false);
    }

    public void SwitchToCredits()
    {
        MainContainer.SetActive(false);
        CreditsContainer.SetActive(true);
        StartContainer.SetActive(false);
        TutorialContainer.SetActive(false);
    }

    public void SwitchToStart()
    {
        MainContainer.SetActive(false);
        CreditsContainer.SetActive(false);
        StartContainer.SetActive(true);
        TutorialContainer.SetActive(false);
    }

    public void SwitchToTutorial()
    {
        MainContainer.SetActive(false);
        CreditsContainer.SetActive(false);
        StartContainer.SetActive(false);
        TutorialContainer.SetActive(true);
    }

    public void StartGame(int difficulty)
    {
        if (LoadingGame) return;
        LoadingGame = true;
        GameMaster.Singleton.StartFirstFloor(difficulty);
    }

    public void OnSliderChanged(float value)
    {
        Mixer.SetFloat("volume", Mathf.Log10(value + float.Epsilon) * 20);
    }

    public void OnFlipYChanged(bool state)
    {
        CustomInput.InvertY = state;
    }
}
