using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{

    [SerializeField] List<GameObject> mainButtonsToDisable = new List<GameObject>(); // any buttons we want to disable from the main canvas
    public GameObject settingsCanvas;
    public GameObject shopCanvas;
    public GameObject tutorialCanvas;
    public GameObject customizeCanvas;

    public GameObject currencyText;

    private void Start()
    {
        Time.timeScale = 1f; // timescale will always be 1 in the menus
        MusicPlayer.instance.aSource.pitch = 1f;
        if (PlayerPrefs.GetInt("savedFirstRun") == 0)
        {
            InitializeData();
        }

        Debug.Log("The player's currency is currently " + PlayerPrefs.GetInt("currency"));
        Debug.Log("High score: " + PlayerPrefs.GetInt("highScore"));
        Debug.Log("Highest Wave Reached: " + PlayerPrefs.GetInt("highWave"));
    }

    void InitializeData()
    {
        PlayerPrefs.SetInt("highScore", 0);
        PlayerPrefs.SetInt("highWave", 0);
        PlayerPrefs.SetInt("currency", 0);
    }

    public void StartGame()
    {
        SaveSoundSettings();
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ActivateMenuCanvas()
    {
        EnableMainButtons();

        // any added canvases need to be added here
        settingsCanvas.SetActive(false);
        shopCanvas.SetActive(false);
        tutorialCanvas.SetActive(false);
        customizeCanvas.SetActive(false);
    }

    public void ActivateSettingsCanvas()
    {
        settingsCanvas.SetActive(true);
        DisableMainButtons();
    }

    public void ActivateShopCanvas()
    {
        shopCanvas.SetActive(true);
        DisableMainButtons();
    }

    public void ActivateTutorialCanvas()
    {
        tutorialCanvas.SetActive(true);
        Invoke("BackToMenu", 3f);
        settingsCanvas.SetActive(false); // parent menu / where we got to the controls canvas
    }

    void BackToMenu()
    {
        tutorialCanvas.SetActive(false);
        EnableMainButtons();
    }

    public void CustomizeCanvas()
    {
        customizeCanvas.SetActive(true);
        DisableMainButtons();
    }

    public void GiveMoney()
    {
        PlayerPrefs.SetInt("currency", PlayerPrefs.GetInt("currency") + 500);
        Debug.Log("Added 500 currency.");
    }

    void SaveSoundSettings()
    {
        PlayerPrefs.SetFloat("musicVolume", MusicPlayer.instance.aSource.volume);
        PlayerPrefs.SetFloat("soundVolume", SoundPlayer.instance.aSource.volume);
    }

    public void InteractSound()
    {
        SoundPlayer.PlayInteractSound();
    }

    void DisableMainButtons()
    {
        foreach (GameObject obj in mainButtonsToDisable)
        {
            obj.SetActive(false);
        }
    }

    void EnableMainButtons()
    {
        foreach (GameObject obj in mainButtonsToDisable)
        {
            obj.SetActive(true);
        }
    }
}