using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System;
using UnityEditor;

public class UIMainMenuManager : MonoBehaviour
{
    public GameObject playersCountObject;
    public GameObject difficultyObject;
    public GameObject mainMenuObject;
    public GameObject gameSettingsObject;
    public GameObject canvasObject;
    public GameObject settingsMenuObject;
    public GameObject helpObject;
    public GameObject playedSessionsObject;
    public GameObject avgTimeObject;
    public GameObject avgTurnsObject;
    public GameObject avgPlayedCardsObject;
    public GameObject avgCardsPerTurnObject;
    public GameObject winSessionsObject;
    public GameObject loseSessionsObject;
    public GameObject winPercentageObject;
    public TextMeshProUGUI tmProPlayedSessions;
    public TextMeshProUGUI tmProAvgTime;
    public TextMeshProUGUI tmProAvgTurns;
    public TextMeshProUGUI tmProAvgPlayedCards;
    public TextMeshProUGUI tmProAvgCardsPerTurn;
    public TextMeshProUGUI tmProWinSessions;
    public TextMeshProUGUI tmProLoseSessions;
    public TextMeshProUGUI tmProWinPercentage;
    public TextMeshProUGUI tmProPlayersCount;
    public TextMeshProUGUI tmProDifficulty;
    public CanvasScaler canvasScaler;
    public string playersCount;
    bool hardDifficulty = false;
    void Awake()
    {
        DOTween.KillAll();

        tmProPlayedSessions = playedSessionsObject.GetComponent<TextMeshProUGUI>();
        tmProAvgTime = avgTimeObject.GetComponent<TextMeshProUGUI>();
        tmProAvgTurns = avgTurnsObject.GetComponent<TextMeshProUGUI>();
        tmProAvgPlayedCards = avgPlayedCardsObject.GetComponent<TextMeshProUGUI>();
        tmProAvgCardsPerTurn = avgCardsPerTurnObject.GetComponent<TextMeshProUGUI>();
        tmProWinSessions = winSessionsObject.GetComponent<TextMeshProUGUI>();
        tmProLoseSessions = loseSessionsObject.GetComponent<TextMeshProUGUI>();
        tmProWinPercentage = winPercentageObject.GetComponent<TextMeshProUGUI>();

        tmProPlayersCount = playersCountObject.GetComponent<TextMeshProUGUI>();
        playersCount = tmProPlayersCount.text;

        tmProDifficulty = difficultyObject.GetComponent<TextMeshProUGUI>();
        hardDifficulty = tmProDifficulty.text == "сложная";

        canvasScaler = canvasObject.GetComponent<CanvasScaler>();
    }
    public void ExitApp()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("PlayersCount", int.Parse(playersCount) + 1);
        SceneManager.LoadScene("Singleplayer");
    }

    public void BackToMainMenu()
    {
        gameSettingsObject.SetActive(false);
        helpObject.SetActive(false);
        settingsMenuObject.SetActive(false);
        mainMenuObject.SetActive(true);
    }

    public void Singleplayer()
    {
        mainMenuObject.SetActive(false);
        helpObject.SetActive(false);
        settingsMenuObject.SetActive(false);
        gameSettingsObject.SetActive(true);
    }

    public void IncPlayersCount()
    {
        int.TryParse(playersCount, out int playersCountToSet);

        if (playersCountToSet >= 1 && playersCountToSet <= 3)
            playersCountToSet++;
        else
            playersCountToSet = 1;

        playersCount = playersCountToSet.ToString();
        tmProPlayersCount.text = playersCount;
    }
    public void DecPlayersCount()
    {
        int.TryParse(playersCount, out int playersCountToSet);

        if (playersCountToSet >= 2 && playersCountToSet <= 4)
            playersCountToSet--;
        else
            playersCountToSet = 4;

        playersCount = playersCountToSet.ToString();
        tmProPlayersCount.text = playersCount;
    }

    public void SetDifficulty()
    {
        if (hardDifficulty)
            tmProDifficulty.text = "стандартная";
        else
            tmProDifficulty.text = "сложная";

        hardDifficulty = !hardDifficulty;
    }

    public void SettingsMenu()
    {
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        
        tmProPlayedSessions.text = PlayerPrefs.GetInt("PlayedSessions").ToString();

        TimeSpan timeSpan = TimeSpan.FromSeconds(PlayerPrefs.GetFloat("AvgTime"));
        tmProAvgTime.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);

        tmProAvgTurns.text = PlayerPrefs.GetFloat("AvgTurns").ToString("F3");
        tmProAvgPlayedCards.text = PlayerPrefs.GetFloat("AvgPlayedCards").ToString("F3");
        tmProAvgCardsPerTurn.text = PlayerPrefs.GetFloat("AvgCardsPerTurn").ToString("F3");
        tmProWinSessions.text = PlayerPrefs.GetInt("WinSessions").ToString();
        tmProLoseSessions.text = PlayerPrefs.GetInt("LoseSessions").ToString();
        tmProWinPercentage.text = PlayerPrefs.GetFloat("WinPercentage").ToString("F3") + "%";

        mainMenuObject.SetActive(false);
        gameSettingsObject.SetActive(false);
        helpObject.SetActive(false);
        settingsMenuObject.SetActive(true);
    }

    public void ResetStats()
    {
        PlayerPrefs.DeleteKey("PlayedSessions");
        PlayerPrefs.DeleteKey("AvgTurns");
        PlayerPrefs.DeleteKey("AvgTime");
        PlayerPrefs.DeleteKey("AvgPlayedCards");
        PlayerPrefs.DeleteKey("AvgCardsPerTurn");
        PlayerPrefs.DeleteKey("WinSessions");
        PlayerPrefs.DeleteKey("LoseSessions");
        PlayerPrefs.DeleteKey("WinPercentage");

        SettingsMenu();
    }

    public void Help()
    {
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        settingsMenuObject.SetActive(false);
        helpObject.SetActive(true);
    }
}
