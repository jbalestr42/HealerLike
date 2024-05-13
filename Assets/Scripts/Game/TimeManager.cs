using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    GameView _gameView;

    void Start()
    {
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.gameHUD.playSpeedx0Button.onClick.AddListener(() => ChangeTimeScale(0f));
        _gameView.gameHUD.playSpeedx05Button.onClick.AddListener(() => ChangeTimeScale(0.5f));
        _gameView.gameHUD.playSpeedx1Button.onClick.AddListener(() => ChangeTimeScale(1f));
        _gameView.gameHUD.playSpeedx2Button.onClick.AddListener(() => ChangeTimeScale(2f));
        _gameView.gameHUD.playSpeedx3Button.onClick.AddListener(() => ChangeTimeScale(3f));
    }

    public void ChangeTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}