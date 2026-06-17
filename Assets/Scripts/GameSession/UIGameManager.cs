using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;

public class UIGameManager : MonoBehaviour
{
    public GameObject gameManagerObject;
    public GameObject settingsObject;
    public GameObject canvasObject;
    public GameObject helpObject;
    public GameObject gameResultsObject;
    public GameObject resultsTextObject;
    public GameObject turnsCountObject;
    public GameObject playedCardsCountObject;
    public GameObject timeObject;
    public GameManager gameManager;
    public List<GameObject> settingsElements;
    public List<SpriteRenderer> settingsElementsSpriteRenderers = new();
    public CanvasScaler canvasScaler;
    public TextMeshProUGUI tmProResultsText;
    public TextMeshProUGUI tmProTurnsCount;
    public TextMeshProUGUI tmProTime;
    public TextMeshProUGUI tmProPlayedCardsCount;

    void Awake()
    {
        gameManager = gameManagerObject.GetComponent<GameManager>();
        canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        tmProResultsText = resultsTextObject.GetComponent<TextMeshProUGUI>();
        tmProTurnsCount = turnsCountObject.GetComponent<TextMeshProUGUI>();
        tmProTime = timeObject.GetComponent<TextMeshProUGUI>();
        tmProPlayedCardsCount = playedCardsCountObject.GetComponent<TextMeshProUGUI>();

        settingsElementsSpriteRenderers = new();

        foreach (var el in settingsElements)
            settingsElementsSpriteRenderers.Add(el.GetComponent<SpriteRenderer>());
    }

    public void Restart()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("Singleplayer");
    }

    public void Exit()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");
    }

    public void Settings()
    {
        bool activate = settingsElementsSpriteRenderers[0].color.a != 1;
        settingsElementsSpriteRenderers[0].color = activate ? Color.white : new(1, 1, 1, 0.05f);

        var list = settingsElements.GetRange(1, settingsElements.Count - 1);

        foreach (var el in list)
            el.SetActive(activate);
    }

    public void Help()
    {
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        settingsObject.SetActive(false);
        gameManager.playedCardsObject.SetActive(false);
        gameManager.playersObject.SetActive(false);
        gameManager.deck.gameObject.SetActive(false);
        gameResultsObject.SetActive(false);
        helpObject.SetActive(true);
    }

    public void ExitHelp()
    {
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        settingsObject.SetActive(true);
        gameManager.playedCardsObject.SetActive(true);
        gameManager.playersObject.SetActive(true);
        gameManager.deck.gameObject.SetActive(true);
        gameResultsObject.SetActive(false);
        helpObject.SetActive(false);
    }

    public void ResultsScreen(bool win, int turnsCount, int playedCardsCount, float time)
    {
        tmProResultsText.text = win ? "победа" : "поражение";
        tmProTurnsCount.text = turnsCount.ToString();
        tmProPlayedCardsCount.text = playedCardsCount.ToString();

        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        tmProTime.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);

        settingsObject.SetActive(false);
        gameManager.playedCardsObject.SetActive(false);
        gameManager.playersObject.SetActive(false);
        gameManager.deck.gameObject.SetActive(false);
        gameResultsObject.SetActive(true);
        helpObject.SetActive(false);
    }
}
