using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

// A run climbs a Slay the Spire like map: the player picks the next room on the map,
// fights, rests, loots or plays an event in it, and goes back to the map until the boss
public class AscensionGameType : AGameType, IEventRoomHost
{
    [HideInInspector] public static UnityEvent OnRoundStart = new UnityEvent();
    [HideInInspector] public static UnityEvent OnBattleStart = new UnityEvent();
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
        PlayEvent,
        Rest,
        GameEnd,
        GameOver,
    }

    [Header("Run")]
    [SerializeField] MapGenerationSettings _mapSettings;
    // 0 to get a different map every run
    [SerializeField] int _seed = 0;
    // Applied to every ally when healing in a rest room
    [SerializeField] AConsumerFactory _restHealConsumer;
    // Applied to the character in every rest room, whatever the choice
    [SerializeField] AConsumerFactory _restManaConsumer;
    [SerializeField, TextArea] string _restDescription = "You recover 30% of your max mana.";
    [SerializeField, TextArea] string _restHealDescription = "Every unit recovers 30% of its max health.";
    [SerializeField, Min(1)] int _rewardChoiceCount = 3;
    [SerializeField, Min(1)] int _eliteRewardChoiceCount = 4;

    [Header("In Game")]
    [ShowInInspector, ReadOnly] State _state = State.None;
    EntityManager _entities = null;
    GameView _gameView;
    UpgradeView _upgradeView;
    MapView _mapView;
    EventView _eventView;
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
        _eventView = UIManager.instance.GetView<EventView>(ViewType.Event);

        _gameView.gameHUD.nextWaveButton.onClick.AddListener(StartBattle);
        if (_gameView.gameHUD.mapButton != null)
        {
            _gameView.gameHUD.mapButton.onClick.AddListener(LookAtMap);
            _gameView.gameHUD.mapButton.interactable = false;
        }
        _upgradeView.OnItemSelected.AddListener(OnItemSelected);
        _upgradeView.OnPlayerItemSelected.AddListener(OnPlayerItemSelected);
        _upgradeView.OnEntitySelected.AddListener(OnEntitySelected);
        _mapView.OnNodeSelected.AddListener(OnRoomSelected);
        _entities.OnEntityKilled.AddListener(OnEntityKilled);
    }

    void OnDestroy()
    {
        if (_entities != null)
        {
            _entities.OnEntityKilled.RemoveListener(OnEntityKilled);
        }
    }

    // A dead ally is lost for the run: its items go back to the player, and it can be resurrected later
    void OnEntityKilled(Entity entity)
    {
        if (_run == null || !IsLostForTheRun(entity, DataManager.instance.GetTagWithName(EntityManager.summonTagName)))
        {
            return;
        }

        _gameView.playerInventory.TakeItemsOf(entity);
        _run.AddDeadAlly(entity.data);
    }

    // Allies only, summons aside: they only last for the battle anyway
    public static bool IsLostForTheRun(Entity entity, GameplayTag summonTag)
    {
        return entity != null && entity.entityType == Entity.EntityType.Player && !entity.HasTag(summonTag);
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

                PlayerBehaviour.instance.Init(DataManager.instance.GetCharacter(CharacterSelection.selected));
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
                OnRoundStart.Invoke();
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
                OnBattleStart.Invoke();
                break;

            case State.OnGoingBattle:
                if (_entities.AreAllEntityDead(Entity.EntityType.Computer))
                {
                    if (IsRunWon(_run.currentNode.type))
                    {
                        WinRun();
                    }
                    else
                    {
                        SetState(State.EndBattle);
                    }
                }
                else if (_entities.AreAllEntityDead(Entity.EntityType.Player))
                {
                    SetState(State.GameOver);
                }
                break;

            case State.EndBattle:
                // Nothing fights outside a battle: no skill, no periodic buff while picking a reward or on the map
                EnableAllEntities(false);
                _entities.RemoveSummons();
                // Reset all unit to their default state (remove temporary buffs)
                ResetAllEntities();
                OnRoundEnd.Invoke();
                ShowRewards(_run.currentNode.type == MapNodeType.Elite ? _eliteRewardChoiceCount : _rewardChoiceCount);
                break;

            case State.SelectUpgrade:
                // Wait for player to select an item then go back to the map
                break;

            case State.PlayEvent:
                // Wait for the event to end (EndEvent) then go back to the map
                break;

            case State.Rest:
                // Wait for the player to heal or resurrect then go back to the map
                break;

            case State.GameEnd:
                // The run is won, the end screen waits for the player to go back to the menu
                break;

            case State.GameOver:
                UIManager.instance.AddView(ViewType.GameOver);
                UIManager.instance.GetView<GameOverView>(ViewType.GameOver).ShowResult(false);
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
                EnterRestRoom();
                break;

            case MapNodeType.Boss:
                _currentWave = DataManager.instance.GetWavePattern(node.type, node.floor, _random);
                SetState(State.InitializeRound);
                break;

            case MapNodeType.Event:
                EnterEventRoom(node);
                break;
        }
    }

    // One of the events of the map settings, drawn by its chance; a combat when there is none to draw
    void EnterEventRoom(MapNode node)
    {
        AEventRoom eventRoom = EventRoomPicker.Pick(_mapSettings.eventRooms, _random);
        if (eventRoom == null)
        {
            Debug.LogWarning("[AscensionGameType] No event to draw in the map settings, the event room is played as a combat");
            _currentWave = DataManager.instance.GetWavePattern(MapNodeType.Combat, node.floor, _random);
            SetState(State.InitializeRound);
            return;
        }

        Debug.Log($"[AscensionGameType] Event room: {eventRoom.name}");
        SetState(State.PlayEvent);
        eventRoom.Play(this);
    }

    public CharacterData characterData => PlayerBehaviour.instance.character.data;

    public System.Random random => _random;

    public void AddUnit(EntityData unit)
    {
        _gameView.entityInventory.AddEntity(unit);
    }

    public IReadOnlyList<AItemFactory> GetItems(List<string> includedTags, List<string> excludedTags = null)
    {
        return DataManager.instance.GetItems(includedTags, excludedTags);
    }

    public void AddPlayerItem(AItem item)
    {
        PlayerBehaviour.instance.character.inventoryHandler.AddItem(item, -1);
    }

    public void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        UIManager.instance.AddView(ViewType.Event);
        _eventView.Display(title, description, CloseBeforeEachChoice(choices, () => UIManager.instance.PopCurrentView()));
    }

    // Copies of the choices calling close before their own action, e.g. so a choice can open another screen
    public static List<EventChoice> CloseBeforeEachChoice(IReadOnlyList<EventChoice> choices, System.Action close)
    {
        List<EventChoice> closingChoices = new List<EventChoice>(choices.Count);
        foreach (EventChoice choice in choices)
        {
            System.Action onSelected = choice.onSelected;
            closingChoices.Add(new EventChoice
            {
                label = choice.label,
                description = choice.description,
                isAvailable = choice.isAvailable,
                onSelected = () =>
                {
                    close();
                    onSelected?.Invoke();
                },
            });
        }
        return closingChoices;
    }

    public void EndEvent()
    {
        if (_state != State.PlayEvent)
        {
            return;
        }

        SetState(State.ShowMap);
    }

    // Beating the boss wins the run, the other fights lead to a reward
    public static bool IsRunWon(MapNodeType roomType)
    {
        return roomType == MapNodeType.Boss;
    }

    void WinRun()
    {
        Debug.Log("[AscensionGameType] Boss defeated, the run is won");
        EnableAllEntities(false);
        UIManager.instance.AddView(ViewType.GameOver);
        UIManager.instance.GetView<GameOverView>(ViewType.GameOver).ShowResult(true);
        SetState(State.GameEnd);
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
        _upgradeView.FillChoices(GetRewardChoiceCount(choiceCount, PlayerBehaviour.instance.character.gameObject));
        SetState(State.SelectUpgrade);
    }

    // The choices of the room, plus the ones the character gets from its RewardChoices (e.g. the Merchant's
    // Ledger), always at least one so a reward can still be picked (attributes never go below 0 for now, so
    // RewardChoices can't take choices away yet)
    public static int GetRewardChoiceCount(int roomChoiceCount, GameObject character)
    {
        AttributeManager attributes = character != null ? character.GetComponent<AttributeManager>() : null;
        if (attributes == null || !attributes.Has(AttributeType.RewardChoices))
        {
            return roomChoiceCount;
        }
        return Mathf.Max(1, roomChoiceCount + Mathf.RoundToInt(attributes.Get(AttributeType.RewardChoices).Value));
    }

    public void OnItemSelected(AItem item)
    {
        _gameView.playerInventory.AddItem(item);
        CloseRewards();
    }

    public void OnPlayerItemSelected(AItem item)
    {
        AddPlayerItem(item);
        CloseRewards();
    }

    // The unit joins the ones the player can place on the grid
    public void OnEntitySelected(EntityData entity)
    {
        AddUnit(entity);
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

    #region Rest

    // Mana back whatever the choice, then heal every unit or resurrect a dead one
    void EnterRestRoom()
    {
        RestoreMana();
        SetState(State.Rest);
        ShowRestChoices();
    }

    void RestoreMana()
    {
        if (_restManaConsumer == null)
        {
            Debug.LogError("[AscensionGameType] No rest mana consumer set");
            return;
        }

        Character character = PlayerBehaviour.instance.character;
        ApplyConsumer(character.mana, character.gameObject, _restManaConsumer);
    }

    void ShowRestChoices()
    {
        ShowChoices("Rest", _restDescription, CreateRestChoices(_restHealDescription, _run.deadAllies.Count > 0, () =>
        {
            HealAllies();
            LeaveRestRoom();
        }, ShowResurrectChoices));
    }

    void ShowResurrectChoices()
    {
        ShowChoices("Resurrect", "Choose the unit to bring back.", CreateResurrectChoices(_run.deadAllies, Resurrect, ShowRestChoices));
    }

    // Back to the unit inventory, at full health but without its items: they went to the player when it died
    void Resurrect(EntityData ally)
    {
        if (_run.RemoveDeadAlly(ally))
        {
            AddUnit(ally);
        }
        LeaveRestRoom();
    }

    void LeaveRestRoom()
    {
        if (_state == State.Rest)
        {
            SetState(State.ShowMap);
        }
    }

    // Resurrect can only be picked when a unit is dead
    public static List<EventChoice> CreateRestChoices(string healDescription, bool canResurrect, System.Action heal, System.Action resurrect)
    {
        return new List<EventChoice>
        {
            new EventChoice { label = "Heal", description = healDescription, onSelected = heal },
            new EventChoice
            {
                label = "Resurrect",
                description = canResurrect ? "Bring a dead unit back, without its items." : "No unit is dead.",
                isAvailable = canResurrect,
                onSelected = resurrect,
            },
        };
    }

    // One choice per dead unit (once even when several copies of it died), then a way back to the rest choices
    public static List<EventChoice> CreateResurrectChoices(IReadOnlyList<EntityData> deadAllies, System.Action<EntityData> resurrect, System.Action back)
    {
        List<EventChoice> choices = new List<EventChoice>();
        HashSet<EntityData> listed = new HashSet<EntityData>();
        foreach (EntityData ally in deadAllies)
        {
            if (ally == null || !listed.Add(ally))
            {
                continue;
            }

            choices.Add(new EventChoice { label = ally.title, description = ally.description, onSelected = () => resurrect(ally) });
        }
        choices.Add(new EventChoice { label = "Back", onSelected = back });
        return choices;
    }

    #endregion

    // Goes through the regular resource flow, so the consumer events and feedbacks are triggered
    public static void ApplyConsumer(Entity entity, AConsumerFactory consumerFactory)
    {
        ApplyConsumer(entity.health, entity.gameObject, consumerFactory);
    }

    // On any resource of the owner, e.g. the mana of the character
    public static void ApplyConsumer(ResourceAttribute resource, GameObject owner, AConsumerFactory consumerFactory)
    {
        resource.AddResourceModifier(ResourceModifier.Create(consumerFactory, owner, owner));
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

        EntityManager.instance.SpawnWave(waveData, transform.position, Entity.EntityType.Computer);
    }
}
