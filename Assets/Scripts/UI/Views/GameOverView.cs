using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverView : AView
{
    [SerializeField] Button _restartButton;

    void Start()
    {
        _restartButton.onClick.AddListener(RestartGame);
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