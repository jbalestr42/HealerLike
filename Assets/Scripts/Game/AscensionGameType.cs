using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

// A run climbs a Slay the Spire like map: the player picks the next room on the map,
// fights, rests or loots it, and goes back to the map until the boss
public class AscensionGameType : AGameType
{
    [HideInInspector] public static UnityEvent OnRoundEnd = new UnityEvent();

    public enum State
    {
        None,
        InitializeGame,
        ShowMap,
        SelectRoom,
        InitializeRound,
        WaitForRoundToStart,
        StartBattle,
        OnGoingBattle,
        EndBattle,
        SelectUpgrade,
        GameEnd,
        GameOver,
    }

    [SerializeField] bool _debug = false;

    [Header("Run")]
    [SerializeField] MapGenerationSettings _mapSettings;
    // 0 to get a different map every run
    [SerializeField] int _seed = 0;
    // Applied to every ally in a rest room
    [SerializeField] AConsumerFactory _restHealConsumer;
    [SerializeField, Min(1)] int _rewardChoiceCount = 3;
    [SerializeField, Min(1)] int _eliteRewardChoiceCount = 4;

    [Header("In Game")]
    [ShowInInspector, ReadOnly] State _state = State.None;
    EntityManager _entities = null;
    GameView _gameView;
    UpgradeView _upgradeView;
    MapView _mapView;
    System.Random _random;

    RunState _run;
    public RunState run => _run;

    WavePatternData _currentWave;

    // Rooms climbed so far, the room being played included
    public int currentRound => _run != null ? _run.currentFloor + 1 : 0;

    void Start()
    {
        _entities = EntityManager.instance;
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.characterSkillInventory.Show(false);
        _gameView.entityInventory.Show(false);
        _gameView.gameHUD.ShowManaBar(false);

        _upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        _mapView = UIManager.instance.GetView<MapView>(ViewType.Map);

        _gameView.gameHUD.nextWaveButton.onClick.AddListener(StartBattle);
        if (_gameView.gameHUD.mapButton != null)
        {
            _gameView.gameHUD.mapButton.onClick.AddListener(LookAtMap);
            _gameView.gameHUD.mapButton.interactable = false;
        }
        _upgradeView.OnItemSelected.AddListener(OnItemSelected);
        _upgradeView.OnPlayerItemSelected.AddListener(OnPlayerItemSelected);
        _mapView.OnNodeSelected.AddListener(OnRoomSelected);

        if (_debug)
        {
            foreach (var item in DataManager.instance.GetItemsWithTag("Entity"))
            {
                _gameView.playerInventory.AddItem(item.GetItem());
            }
        }
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

                PlayerBehaviour.instance.Init(DataManager.instance.GetRandomCharacter());
                _gameView.entityInventory.Init(PlayerBehaviour.instance.character.entityPool);

                int seed = _seed != 0 ? _seed : System.Environment.TickCount;
                Debug.Log($"[AscensionGameType] Run seed: {seed}");
                _random = new System.Random(seed);
                _run = new RunState(MapGenerator.Generate(_mapSettings, seed));

                SetState(State.ShowMap);
                break;

            case State.ShowMap:
                _gameView.gameHUD.nextWaveButton.interactable = false;
                SetMapButtonInteractable(false);
                UIManager.instance.AddView(ViewType.Map);
                _mapView.Display(_run, true);
                SetState(State.SelectRoom);
                break;

            case State.SelectRoom:
                // Wait for the player to click on one of the next rooms of the map
                break;

            case State.InitializeRound:
                _gameView.gameHUD.inventoryButton.interactable = true;
                _gameView.gameHUD.nextWaveButton.interactable = true;
                SetMapButtonInteractable(true);
                _gameView.characterSkillInventory.Show(true);
                _gameView.entityInventory.Show(true);

                LoadEnemies(_currentWave);
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
                SetMapButtonInteractable(false);
                _gameView.entityInventory.Show(false);
                _gameView.playerInventory.HideInventory();
                EnableAllEntities(true);
                // TODO: Show countdown before starting the battle
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
                // Reset all unit to their default state (remove temporary buffs)
                ResetAllEntities();
                OnRoundEnd.Invoke();
                ShowRewards(_run.currentNode.type == MapNodeType.Elite ? _eliteRewardChoiceCount : _rewardChoiceCount);
                break;

            case State.SelectUpgrade:
                // Wait for player to select an item then go back to the map
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

    public override bool IsOver()
    {
        return _state == State.GameEnd;
    }

    void OnRoomSelected(MapNode node)
    {
        if (_state != State.SelectRoom || !_run.TravelTo(node))
        {
            return;
        }

        UIManager.instance.PopCurrentView();
        EnterRoom(node);
    }

    void EnterRoom(MapNode node)
    {
        switch (node.type)
        {
            case MapNodeType.Combat:
            case MapNodeType.Elite:
                _currentWave = DataManager.instance.GetWavePattern(node.type, node.floor, _random);
                SetState(State.InitializeRound);
                break;

            case MapNodeType.Treasure:
                ShowRewards(_rewardChoiceCount);
                break;

            case MapNodeType.Rest:
                HealAllies();
                SetState(State.ShowMap);
                break;

            case MapNodeType.Boss:
                // TODO: boss fight, the run is won for now
                Debug.Log("[AscensionGameType] Boss reached, the run is over");
                SetState(State.GameEnd);
                break;
        }
    }

    // The map can be looked at while arranging the units before a fight
    void LookAtMap()
    {
        if (_state != State.WaitForRoundToStart)
        {
            return;
        }

        UIManager.instance.AddView(ViewType.Map);
        _mapView.Display(_run, false);
    }

    void SetMapButtonInteractable(bool isInteractable)
    {
        if (_gameView.gameHUD.mapButton != null)
        {
            _gameView.gameHUD.mapButton.interactable = isInteractable;
        }
    }

    void ShowRewards(int choiceCount)
    {
        UIManager.instance.AddView(ViewType.Upgrade);
        _upgradeView.FillChoices(choiceCount);
        SetState(State.SelectUpgrade);
    }

    public void OnItemSelected(AItem item)
    {
        _gameView.playerInventory.AddItem(item);
        CloseRewards();
    }

    public void OnPlayerItemSelected(AItem item)
    {
        PlayerBehaviour.instance.character.inventoryHandler.AddItem(item, -1);
        CloseRewards();
    }

    void CloseRewards()
    {
        _upgradeView.ClearChoices();
        UIManager.instance.PopCurrentView();
        SetState(State.ShowMap);
    }

    void SetState(State newState)
    {
        Debug.Log($"[AscensionGameType] {_state} -> {newState}");
        _state = newState;
    }

    void HealAllies()
    {
        if (_restHealConsumer == null)
        {
            Debug.LogError("[AscensionGameType] No rest heal consumer set");
            return;
        }

        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => ApplyConsumer(x.GetComponent<Entity>(), _restHealConsumer));
    }

    // Goes through the regular resource flow, so the consumer events and feedbacks are triggered
    public static void ApplyConsumer(Entity entity, AConsumerFactory consumerFactory)
    {
        entity.health.AddResourceModifier(ResourceModifier.Create(consumerFactory, entity.gameObject, entity.gameObject));
    }

    void EnableAllEntities(bool isEnabled)
    {
        PlayerBehaviour.instance.character.Enable(isEnabled);
        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => x.GetComponent<Entity>().Enable(isEnabled));
        _entities.GetEntities(Entity.EntityType.Computer).ForEach(x => x.GetComponent<Entity>().Enable(isEnabled));
    }

    void ResetAllEntities()
    {
        PlayerBehaviour.instance.character.Reset();
        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => x.GetComponent<Entity>().Reset());
        _entities.GetEntities(Entity.EntityType.Computer).ForEach(x => x.GetComponent<Entity>().Reset());
    }

    public void LoadEnemies(WavePatternData waveData)
    {
        if (waveData == null)
        {
            return;
        }

        for (int i = 0; i < waveData.width; i++)
        {
            for (int j = 0; j < waveData.height; j++)
            {
                if (waveData.slots[i, j].entity != null)
                {
                    EntityManager.instance.SpawnEntity(waveData.slots[i, j].entity, transform.position - new Vector3(waveData.width / 2f, 0f, waveData.height / 2f) + new Vector3(i, 0f, j), Entity.EntityType.Computer);
                }
            }
        }
    }
}
