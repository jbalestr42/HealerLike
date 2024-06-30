using UnityEngine;
using Sirenix.OdinInspector;

public class AscensionGameType : AGameType
{
    public enum State
    {
        None,
        InitializeGame,
        InitializeRound,
        WaitForRoundToStart,
        StartBattle,
        OnGoingBattle,
        EndBattle,
        SelectUpgrade,
        GameEnd,
        GameOver,
    }

    [Header("In Game")]
    [ShowInInspector, ReadOnly] State _state = State.None;
    EntityManager _entities = null;
    GameView _gameView;
    UpgradeView _upgradeView;
    int _currentRound = 0;

    public override int currentWave => _currentRound;

    void Start()
    {
        _entities = EntityManager.instance;
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.characterSkillInventory.Show(false);
        _gameView.entityInventory.Show(false);
        _gameView.gameHUD.ShowManaBar(false);

        _upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);

        _gameView.gameHUD.nextWaveButton.onClick.AddListener(StartBattle);
        _upgradeView.OnItemSelected.AddListener(OnItemSelected);
    }

    void Update()
    {
        switch (_state)
        {
            case State.None:
                break;

            case State.InitializeGame:
                _gameView.gameHUD.inventoryButton.enabled = true;
                _gameView.gameHUD.ShowManaBar(true);

                PlayerBehaviour.instance.Init();
                _gameView.entityInventory.Init(PlayerBehaviour.instance.character.entityPool);

                SetState(State.InitializeRound);
                break;

            case State.InitializeRound:
                _currentRound++;
                _gameView.gameHUD.inventoryButton.interactable = true;
                _gameView.gameHUD.nextWaveButton.interactable = true;
                _gameView.characterSkillInventory.Show(false);
                _gameView.entityInventory.Show(true);

                // Later we can show multiple choice to the user
                LoadEnemies(DataManager.instance.GetRandomWavePattern());
                EnableAllEntities(false);
                SetState(State.WaitForRoundToStart);
                break;

            case State.WaitForRoundToStart:
                // During this time the player can arrange the units and dispatch items
                // Then, wait for player to click on "nextWaveButton" to go to the StartBattle state
                break;

            case State.StartBattle:
                _gameView.gameHUD.inventoryButton.interactable = false;
                _gameView.gameHUD.nextWaveButton.interactable = false;
                _gameView.characterSkillInventory.Show(true);
                _gameView.entityInventory.Show(false);
                _gameView.playerInventory.HideInventory();
                EnableAllEntities(true);
                // Show countdown before starting the battl
                SetState(State.OnGoingBattle);
                break;

            case State.OnGoingBattle:
                if (_entities.AreAllEntityDead(Entity.EntityType.Computer))
                {
                    SetState(State.EndBattle);
                }
                else if (_entities.AreAllEntityDead(Entity.EntityType.Player))
                {
                    SetState(State.GameOver);
                }
                break;

            case State.EndBattle:
                // Show Upgrade UI
                UIManager.instance.AddView(ViewType.Upgrade);
                _upgradeView.FillChoices();
                    SetState(State.SelectUpgrade);
                break;

            case State.SelectUpgrade:
                // Wait for player to select an item then move to InitializeRound
                break;

            case State.GameEnd:
                // do something like restart
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

    void StartBattle()
    {
        SetState(State.StartBattle);
    }

    public override void StartGame()
    {
        SetState(State.InitializeGame);
    }

    public void OnItemSelected(AItem item)
    {
        _gameView.playerInventory.AddItem(item);
        _upgradeView.ClearChoices();
        UIManager.instance.PopCurrentView();
        SetState(State.InitializeRound);
    }

    public override bool IsOver()
    {
        return _state == State.GameEnd;
    }

    void SetState(State newState)
    {
        Debug.Log($"[AscensionGameType] {_state} -> {newState}");
        _state = newState;
    }

    void EnableAllEntities(bool enable)
    {
        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => x.GetComponent<Entity>().isEnabled = enable);
        _entities.GetEntities(Entity.EntityType.Computer).ForEach(x => x.GetComponent<Entity>().isEnabled = enable);
    }

    public void LoadEnemies(WavePatternData waveData)
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