using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        None,
        StartGame,
        Running
    }

    AGameType _gameType;
    public AGameType gameType { get { return _gameType; } }

    GameState _state = GameState.None;

    void Start()
    {
        _gameType = GetComponent<AGameType>();

        UIManager.instance.AddView(ViewType.Game);
        // The character was picked in the menu: the run starts with the scene
        SetState(GameState.StartGame);
        
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    void Update()
    {
        switch (_state)
        {
            case GameState.None:
                break;

            case GameState.StartGame:
                _gameType.StartGame();
                SetState(GameState.Running);
                break;

            case GameState.Running:
                if (_gameType.IsOver())
                {
                    SetState(GameState.None);
                }
                break;

            default:
                Debug.Log("State not implemented: " + _state);
                break;
        }
    }

    void SetState(GameState newState)
    {
        Debug.Log($"[GameManager] {_state} -> {newState}");
        _state = newState;
    }

    void OnEntityKilled(Entity entity)
    {
        GetComponent<Unity.Cinemachine.CinemachineImpulseSource>().GenerateImpulse();
    }
}
