using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button _startGameButton;
    public UnityEngine.UI.Button startGameButton { get { return _startGameButton; } }

    [SerializeField] UnityEngine.UI.Button _inventoryButton;
    public UnityEngine.UI.Button inventoryButton { get { return _inventoryButton; } }

    [SerializeField] UnityEngine.UI.Text _goldText;
    [SerializeField] UnityEngine.UI.Button _playSpeedx0Button;
    public UnityEngine.UI.Button playSpeedx0Button { get { return _playSpeedx0Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx05Button;
    public UnityEngine.UI.Button playSpeedx05Button { get { return _playSpeedx05Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx1Button;
    public UnityEngine.UI.Button playSpeedx1Button { get { return _playSpeedx1Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx2Button;
    public UnityEngine.UI.Button playSpeedx2Button { get { return _playSpeedx2Button; } }
    [SerializeField] UnityEngine.UI.Button _playSpeedx3Button;
    public UnityEngine.UI.Button playSpeedx3Button { get { return _playSpeedx3Button; } }
    [SerializeField] UnityEngine.UI.Toggle _markEnemyToggle;
    public UnityEngine.UI.Toggle markEnemyToggle { get { return _markEnemyToggle; } }

    void Start()
    {
        PlayerBehaviour.instance.OnGoldChanged.AddListener(SetGold);
    }

    public void SetGold(int gold)
    {
        _goldText.text = "Gold: " + gold.ToString();
    }
}
