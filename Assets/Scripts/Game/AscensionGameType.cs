using UnityEngine;

public class AscensionGameType : AGameType
{
    public enum State
    {
        None,
        InitializeWave,
        WaitForWaveToStart,
        WaitingForEndOfWave,
        SelectUpgrade,
        GameEnd,
        GameOver,
    }

    State _state = State.None;
    EntityManager _entities = null;
    GameView _gameView;
    UpgradeView _upgradeView;
    int _currentWave = 0;

    public override int currentWave => _currentWave;

    void Start()
    {
        _entities = EntityManager.instance;
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);

        _gameView.gameHUD.nextWaveButton.onClick.AddListener(StartWave);
        _upgradeView.OnItemSelected.AddListener(OnItemSelected);
    }

    void Update()
    {
        switch (_state)
        {
            case State.None:
                break;

            case State.InitializeWave:
                _currentWave++;
                _gameView.gameHUD.inventoryButton.interactable = true;
                _gameView.gameHUD.nextWaveButton.interactable = true;
                SetState(State.WaitForWaveToStart);
                break;

            case State.WaitForWaveToStart:
                // Wait for player to click on "nextWaveButton"
                break;

            case State.WaitingForEndOfWave:
                if (_entities.AreAllEntityDead(Entity.EntityType.Computer))
                {
                    // Show Upgrade UI
                    UIManager.instance.AddView(ViewType.Upgrade);
                    _upgradeView.FillChoices();
                    SetState(State.SelectUpgrade);
                }
                // else if (_entities.AreAllEntityDead(Entity.EntityType.Player))
                // {
                //     SetState(State.GameOver);
                // }
                break;

            case State.SelectUpgrade:
                    // Wait for player to select an item
                break;

            case State.GameEnd:
                break;

            case State.GameOver:
                UIManager.instance.AddView(ViewType.GameOver);
                SetState(State.None);
                break;

            default:
                Debug.Log("State not implemented: " + _state);
                break;
        }
    }

    void StartWave()
    {
        SetState(State.WaitingForEndOfWave);
        _gameView.gameHUD.inventoryButton.interactable = false;
        _gameView.gameHUD.nextWaveButton.interactable = false;
        _gameView.playerInventory.HideInventory();
        LoadWave(DataManager.instance.GetRandomWavePattern());
    }

    public override void StartGame()
    {
        SetState(State.InitializeWave);
    }

    public void OnItemSelected(AItem item)
    {
        _gameView.playerInventory.AddItem(item);
        _upgradeView.ClearChoices();
        UIManager.instance.PopCurrentView();
        SetState(State.InitializeWave);
    }

    public override bool IsOver()
    {
        return _state == State.GameEnd;
    }

    void SetState(State newState)
    {
        Debug.Log($"[WaveGameType] {_state} -> {newState}");
        _state = newState;
    }

    public void LoadWave(WavePatternData waveData)
    {
        for (int i = 0; i < waveData.width; i++)
        {
            for (int j = 0; j < waveData.height; j++)
            {
                if (waveData.spawns[i, j] != null)
                {
                    GameObject entity = EntityManager.instance.SpawnEntity(waveData.spawns[i, j], transform.position - new Vector3(waveData.width / 2f, 0f, waveData.height / 2f) + new Vector3(i, 0f, j), Entity.EntityType.Computer);
                    if (entity != null)
                    {
                        entity.transform.parent = transform;
                    }
                }
            }
        }
    }
}