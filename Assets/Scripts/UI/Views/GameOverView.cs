using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverView : AView
{
    [SerializeField] Button _restartButton;
    // Optional, the result of the run
    [SerializeField] Text _title;

    void Start()
    {
        _restartButton.onClick.AddListener(RestartGame);
    }

    public void ShowResult(bool isVictory)
    {
        if (_title != null)
        {
            _title.text = GetResultTitle(isVictory);
        }
    }

    public static string GetResultTitle(bool isVictory)
    {
        return isVictory ? "Victory!" : "Game Over";
    }

    void RestartGame()
    {
        //Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene("MenuScene");
    }

    public override void Hide()
    {
    }

    public override void Show()
    {
    }
}