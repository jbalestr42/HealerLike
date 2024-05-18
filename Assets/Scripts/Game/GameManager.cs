using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : Singleton<GameManager>
{
    public UnityEvent<int> OnWaveChanged = new UnityEvent<int>();

    public enum GameState
    {
        None,
        StartGame,
        Running
    }

    AGameType _gameType;
    public AGameType gameType { get { return _gameType; } }

    GameState _state = GameState.None;
    GameView _gameView;

    void Start()
    {
        _gameType = GetComponent<AGameType>();
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);

        _gameView.gameHUD.startGameButton.onClick.AddListener(StartGame);

        UIManager.instance.AddView(ViewType.Game);
        
        EntityManager.instance.OnEnemyKilled.AddListener(OnEnemyKilled);
    }

    void Update()
    {
        switch (_state)
        {
            case GameState.None:
                break;

            case GameState.StartGame:
                _gameView.gameHUD.startGameButton.interactable = false;
                _gameType.StartGame();
                SetState(GameState.Running);
                break;

            case GameState.Running:
                if (_gameType.IsOver())
                {
                    _gameView.gameHUD.startGameButton.interactable = true;
                    SetState(GameState.None);
                }
                break;

            default:
                Debug.Log("State not implemented: " + _state);
                break;
        }
    }

    void StartGame()
    {
        SetState(GameState.StartGame);
    }

    void SetState(GameState newState)
    {
        Debug.Log($"[GameManager] {_state} -> {newState}");
        _state = newState;
    }

    void OnEnemyKilled(Enemy enemy, bool hasReachedEnd)
    {
        if (hasReachedEnd)
        {
            GetComponent<Cinemachine.CinemachineImpulseSource>().GenerateImpulse(enemy.data.lifeCost);
        }
    }
}
