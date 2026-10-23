using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oisif.Inspector;

// A run climbs a Slay the Spire like map: the player picks the next room on the map,
// fights, rests, loots or plays an event in it, and goes back to the map until the boss
public class AscensionGameType : AGameType, IEventRoomHost
{
    [HideInInspector] public static UnityEvent OnRoundStart = new UnityEvent();
    [HideInInspector] public static UnityEvent OnBattleStart = new UnityEvent();
    [HideInInspector] public static UnityEvent OnRoundEnd = new UnityEvent();
    // The next room picked on the map, before it is entered
    [HideInInspector] public static UnityEvent<MapNode> OnRoomSelected = new UnityEvent<MapNode>();

    public enum State
    {
        None,
        InitializeGame,
        ShowMap,
        SelectRoom,
        // The room picked on the map, entered on the next update
        EnterRoom,
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
    [SerializeField, TextArea] string _restDescription = "Take a moment to recover before moving on.";
    [SerializeField, Min(1)] int _rewardChoiceCount = 3;
    // The units offered after an elite, the only way to get one
    [SerializeField, Min(1)] int _eliteUnitChoiceCount = 3;
    // Writes every fight to the combat log (Logs/Balance), off in a normal run
    [SerializeField] bool _recordCombatLog = false;

    [Header("In Game")]
    [ShowInInspector, ReadOnly] State _state = State.None;

    public State state { get { return _state; } }
    EntityManager _entities = null;
    System.Random _random;
    int _seedUsed;
    string _runId;
    // Fills the combat log during a fight, null outside of one
    CombatRecorder _combatRecorder;

    RunState _run;
    public RunState run => _run;

    // The map settings of the run, a copy changed by the ascension level (more elites), or _mapSettings
    MapGenerationSettings _runMapSettings;
    // Resurrected allies not placed on the board yet, the ascension resurrect consumers applied once they are
    List<EntityData> _resurrectedAllies = new List<EntityData>();

    AscensionSettings ascension => DataManager.instance.data.ascension;
    int ascensionLevel => _run != null ? _run.ascensionLevel : 0;

    WavePatternData _currentWave;
    // The kind of fight of the wave: Combat, Elite or Boss (an event played as a combat is a Combat)
    MapNodeType _currentFightType;

    // The choices of the reward on screen, empty outside of one
    List<RewardChoice> _rewardChoices = new List<RewardChoice>();
    public IReadOnlyList<RewardChoice> rewardChoices => _rewardChoices;

    // The choices of the event or rest room on screen, null outside of one
    EventPrompt _currentEvent;
    public EventPrompt currentEvent => _currentEvent;

    // The run ended on a win (the boss beaten), on a loss otherwise
    bool _isVictory = false;
    public bool isVictory => _isVictory;

    // Once the first room is entered: the skills of the character are shown from then on
    bool _hasStartedARound = false;
    public bool hasStartedARound => _hasStartedARound;

    // Arranging the units and giving them items before a fight
    public bool isPreparing => _state == State.WaitForRoundToStart;
    public bool canStartBattle => isPreparing;
    public bool canSelectRoom => _state == State.SelectRoom;
    // The map can be looked at while arranging the units before a fight
    public bool canLookAtMap => isPreparing && screens.current == GameScreen.Game;

    // Taken on Start
    GameManager _game;
    PlayerBehaviour _player;
    GameScreens screens => _game.screens;
    PlayerBehaviour player => _player;

    // Rooms climbed so far, the room being played included
    public int currentRound => _run != null ? _run.currentFloor + 1 : 0;

    void Start()
    {
        _entities = EntityManager.instance;
        _game = GameManager.instance;
        _player = PlayerBehaviour.instance;
        _entities.OnEntityKilled.AddListener(OnEntityKilled);
        _entities.OnEntitySummoned.AddListener(OnEntitySummoned);
        _entities.OnEntitySpawned.AddListener(OnEntitySpawned);
    }

    void OnDestroy()
    {
        if (_entities != null)
        {
            _entities.OnEntityKilled.RemoveListener(OnEntityKilled);
            _entities.OnEntitySummoned.RemoveListener(OnEntitySummoned);
            _entities.OnEntitySpawned.RemoveListener(OnEntitySpawned);
        }
        if (_runMapSettings != null && _runMapSettings != _mapSettings)
        {
            Destroy(_runMapSettings);
        }
    }

    // A dead ally is lost for the run: its items go back to the player, and it can be resurrected later
    void OnEntityKilled(Entity entity)
    {
        _combatRecorder?.RecordDeath(entity);

        if (_run == null || !IsLostForTheRun(entity, DataManager.instance.GetTagWithName(TagNames.Summon)))
        {
            return;
        }

        player.TakeItemsOf(entity);
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
                int runAscensionLevel = GetAscensionLevel(ascension, CharacterSelection.ascensionLevel);
                Debug.Log($"[AscensionGameType] Ascension level: {runAscensionLevel}");

                PlayerBehaviour.instance.Init(DataManager.instance.GetCharacter(CharacterSelection.selected));
                if (ascension != null)
                {
                    GiveItems(PlayerBehaviour.instance.character, ascension.GetCharacterItems(runAscensionLevel));
                }
                player.roster.Init(PlayerBehaviour.instance.character.entityPool);
                if (ascension != null)
                {
                    ReturnToInventory(EntityManager.instance.board.SetCenterBlocked(BoardCellType.Player, ascension.BlocksPlayerCenter(runAscensionLevel)));
                }

                int seed = _seed != 0 ? _seed : System.Environment.TickCount;
                Debug.Log($"[AscensionGameType] Run seed: {seed}");
                _random = new System.Random(seed);
                _seedUsed = seed;
                _runId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                _runMapSettings = GetRunMapSettings(_mapSettings, ascension, runAscensionLevel);
                _run = new RunState(MapGenerator.Generate(_runMapSettings, seed), runAscensionLevel);

                SetState(State.ShowMap);
                break;

            case State.ShowMap:
                // The room can be picked as soon as the map is on screen
                SetState(State.SelectRoom);
                screens.Open(GameScreen.Map);
                break;

            case State.SelectRoom:
                // Wait for the player to click on one of the next rooms of the map
                break;

            case State.EnterRoom:
                EnterRoom(_run.currentNode);
                break;

            case State.InitializeRound:
                _hasStartedARound = true;
                RefillManaForCombat(PlayerBehaviour.instance.character.mana);
                List<Entity> enemies = LoadEnemies(_currentWave);
                if (ascension != null)
                {
                    List<AItemFactory> enemyItems = ascension.GetEnemyItems(ascensionLevel, _currentFightType);
                    enemies.ForEach(enemy => GiveItems(enemy, enemyItems));
                }
                EnableAllEntities(false);
                SetState(State.WaitForRoundToStart);
                OnRoundStart.Invoke();
                break;

            case State.WaitForRoundToStart:
                // During this time the player can arrange the units and dispatch items
                // Then, wait for player to click on "nextWaveButton" to go to the StartBattle state
                break;

            case State.StartBattle:
                EnableAllEntities(true);
                StartCombatLog();
                // TODO: Show countdown before starting the battle
                SetState(State.OnGoingBattle);
                OnBattleStart.Invoke();
                break;

            case State.OnGoingBattle:
                if (_entities.AreAllEntityDead(Entity.EntityType.Computer))
                {
                    FinishCombatLog(true);
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
                    FinishCombatLog(false);
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
                if (GivesUnitReward(_run.currentNode.type))
                {
                    ShowUnitReward();
                }
                else
                {
                    ShowRewards(_rewardChoiceCount);
                }
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
                _isVictory = false;
                SetState(State.None);
                screens.Open(GameScreen.GameOver);
                break;

            default:
                Debug.Log("State not implemented: " + _state);
                break;
        }
    }

    // Once the units are arranged
    public void StartBattle()
    {
        if (!canStartBattle)
        {
            return;
        }

        SetState(State.StartBattle);
    }

#if UNITY_EDITOR
    // Editor only, to skip a fight: every enemy dies and the fight is won on the next update, started first when the
    // units are still being placed
    public void KillEnemyWave()
    {
        if (_state != State.WaitForRoundToStart && _state != State.StartBattle && _state != State.OnGoingBattle)
        {
            return;
        }

        if (_state == State.WaitForRoundToStart)
        {
            StartBattle();
        }
        _entities.KillAll(Entity.EntityType.Computer);
    }
#endif

    public override void StartGame()
    {
        SetState(State.InitializeGame);
    }

    public override bool IsOver()
    {
        return _state == State.GameEnd;
    }

    // One of the next rooms of the map, picked on the map
    public void SelectRoom(MapNode node)
    {
        if (!canSelectRoom || !_run.TravelTo(node))
        {
            return;
        }

        screens.Close();
        SetState(State.EnterRoom);
        OnRoomSelected.Invoke(node);
    }

    void EnterRoom(MapNode node)
    {
        switch (node.type)
        {
            case MapNodeType.Combat:
            case MapNodeType.Elite:
                _currentWave = DataManager.instance.GetWavePattern(node.type, node.floor, _random);
                _currentFightType = node.type;
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
                _currentFightType = node.type;
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
        AEventRoom eventRoom = EventRoomPicker.Pick(_runMapSettings.eventRooms, _random);
        if (eventRoom == null)
        {
            Debug.LogWarning("[AscensionGameType] No event to draw in the map settings, the event room is played as a combat");
            _currentWave = DataManager.instance.GetWavePattern(MapNodeType.Combat, node.floor, _random);
            _currentFightType = MapNodeType.Combat;
            SetState(State.InitializeRound);
            return;
        }

        Debug.Log($"[AscensionGameType] Event room: {eventRoom.name}");
        SetState(State.PlayEvent);
        eventRoom.Play(this);
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
                source = choice.source,
                onSelected = () =>
                {
                    close();
                    onSelected?.Invoke();
                },
            });
        }
        return closingChoices;
    }

    // Every fight gives back the CombatManaRefill share of the max mana (items included) on top of the mana left,
    // capped by the max, while the units keep the health they had at the end of the previous fight. A gain like
    // the others (applied on the mana's next update, the effects listening to the mana gains react to it), but
    // never critical
    public static void RefillManaForCombat(ResourceAttribute characterMana)
    {
        RefillMana(characterMana, GetAttribute(characterMana.gameObject, AttributeType.CombatManaRefill));
    }

    // The tooltip of the mana bar: the mana given back after each fight and at a rest, with the items of the character
    public static string GetManaTooltip(ResourceAttribute characterMana)
    {
        GameObject character = characterMana.gameObject;
        float combatShare = Mathf.Max(0f, GetAttribute(character, AttributeType.CombatManaRefill));
        return GetManaTooltip(characterMana.Max, combatShare, GetRestManaRefill(character));
    }

    public static string GetManaTooltip(float maxMana, float combatShare, float restShare)
    {
        return $"<b>Mana</b>\nAfter each fight, you recover {FormatShare(combatShare)} of your max mana: <b>+{Mathf.RoundToInt(maxMana * combatShare)} mana</b> for the next fight."
            + $"\nResting recovers {FormatShare(restShare)}: <b>+{Mathf.RoundToInt(maxMana * restShare)} mana</b>."
            + "\nNever more than your max mana.";
    }

    static string FormatShare(float share)
    {
        return $"{Mathf.RoundToInt(share * 100f)}%";
    }

    // That share of the max mana back, through the regular resource flow, never critical
    static void RefillMana(ResourceAttribute characterMana, float share)
    {
        ResourceModifier manaModifier = new ResourceModifier { source = characterMana.gameObject };
        manaModifier.consumers.Add(new RuntimeConsumer(characterMana.Max * Mathf.Max(0f, share), canBeCritical: false));
        characterMana.AddResourceModifier(manaModifier);
    }

    // Its default value when the owner doesn't have the attribute
    public static float GetAttribute(GameObject owner, AttributeType type)
    {
        AttributeManager attributes = owner != null ? owner.GetComponent<AttributeManager>() : null;
        return attributes != null && attributes.Has(type) ? attributes.Get(type).Value : AttributeManager.GetDefaultValue(type);
    }

    // The level chosen on the menu, within the levels of the game; 0 when the game has no ascension levels
    public static int GetAscensionLevel(AscensionSettings ascension, int chosenLevel)
    {
        return ascension != null ? ascension.ClampLevel(chosenLevel) : 0;
    }

    // The map settings themselves when the level changes nothing on the map, else a copy with its changes (destroyed
    // by the caller)
    public static MapGenerationSettings GetRunMapSettings(MapGenerationSettings mapSettings, AscensionSettings ascension, int level)
    {
        float eliteWeightMultiplier = ascension != null ? ascension.GetEliteWeightMultiplier(level) : 1f;
        if (Mathf.Approximately(eliteWeightMultiplier, 1f))
        {
            return mapSettings;
        }
        return mapSettings.CloneWithWeight(MapNodeType.Elite, eliteWeightMultiplier);
    }

    // In the inventory of the character, shown with its other items
    public static void GiveItems(Character character, List<AItemFactory> items)
    {
        foreach (AItemFactory item in items)
        {
            character.inventoryHandler.AddItem(item.GetItem(), -1);
        }
        character.ApplyBuffs();
    }

    // In the inventory of the unit, without slot: shown in its first free slots (see SlotInventory.RefreshInventory)
    public static void GiveItems(Entity entity, List<AItemFactory> items)
    {
        foreach (AItemFactory item in items)
        {
            entity.inventoryHandler.AddItem(item.GetItem(), -1);
        }
    }

    // Beating the boss wins the run, the other fights lead to a reward
    public static bool IsRunWon(MapNodeType roomType)
    {
        return roomType == MapNodeType.Boss;
    }

    void WinRun()
    {
        Debug.Log("[AscensionGameType] Boss defeated, the run is won");
        UnlockNextAscensionLevel();
        EnableAllEntities(false);
        _isVictory = true;
        SetState(State.GameEnd);
        screens.Open(GameScreen.GameOver);
    }

    // Saved right away, so the level stays unlocked even if the game is closed on the end screen
    void UnlockNextAscensionLevel()
    {
        AscensionSettings ascension = DataManager.instance.data.ascension;
        if (ascension == null)
        {
            return;
        }

        try
        {
            string path = AscensionProgress.defaultPath;
            AscensionProgress progress = AscensionProgress.Load(path);
            string character = AscensionProgress.GetKey(PlayerBehaviour.instance.character.data);
            if (progress.RecordWin(character, _run.ascensionLevel, ascension.maxLevel))
            {
                progress.Save(path);
                Debug.Log($"[AscensionGameType] Ascension level {progress.GetUnlockedLevel(character)} unlocked for {character}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AscensionGameType] Could not save the ascension progress: {e.Message}");
        }
    }

    // Only to look at it, no room can be picked
    public void LookAtMap()
    {
        if (!canLookAtMap)
        {
            return;
        }

        screens.Open(GameScreen.Map);
    }

    // Back to the units, only from a map opened to look at it
    public void CloseMap()
    {
        if (!isPreparing || screens.current != GameScreen.Map)
        {
            return;
        }

        screens.Close();
    }

    // Beating an elite is the only way to get a unit, the other rooms give items
    public static bool GivesUnitReward(MapNodeType roomType)
    {
        return roomType == MapNodeType.Elite;
    }

    // A choice among a few of the units the character can recruit (plus its RewardChoices), as the Recruit event; the
    // items instead for a character without any unit to recruit
    void ShowUnitReward()
    {
        int count = GetRewardChoiceCount(_eliteUnitChoiceCount, PlayerBehaviour.instance.character.gameObject);
        List<EntityData> units = EventRoomPicker.PickDistinct(GetRewardEntities(), count, _random);
        if (units.Count == 0)
        {
            ShowRewards(_rewardChoiceCount);
            return;
        }

        SetState(State.PlayEvent);
        ShowChoices("Elite defeated", "A unit offers to join your team.", RecruitEventRoom.CreateChoices(units, unit =>
        {
            AddUnit(unit);
            EndEvent();
        }, EndEvent));
    }

    void ShowRewards(int choiceCount)
    {
        Character character = PlayerBehaviour.instance.character;
        _rewardChoices = RewardChoice.Roll(GetRewardChoiceCount(choiceCount, character.gameObject), character.data);
        SetState(State.SelectUpgrade);
        screens.Open(GameScreen.Reward);
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

    // One of the reward choices on screen, then back to the map
    public void SelectReward(RewardChoice choice)
    {
        if (_state != State.SelectUpgrade || !_rewardChoices.Contains(choice))
        {
            return;
        }

        switch (choice.type)
        {
            case RewardChoiceType.Unit:
                AddUnit(choice.unit);
                break;
            case RewardChoiceType.PlayerItem:
                AddPlayerItem(choice.item);
                break;
            case RewardChoiceType.EntityItem:
                AddUnitItem(choice.item);
                break;
        }

        _rewardChoices.Clear();
        screens.Close();
        SetState(State.ShowMap);
    }

    void SetState(State newState)
    {
        Debug.Log($"[AscensionGameType] {_state} -> {newState}");
        _state = newState;
    }

    void HealAllies()
    {
        float share = GetRestHeal(PlayerBehaviour.instance.character.gameObject);
        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => HealForRest(x.GetComponent<Entity>(), share));
    }

    #region Combat log

    void StartCombatLog()
    {
        if (!_recordCombatLog)
        {
            return;
        }

        CombatStats stats = new CombatStats
        {
            runId = _runId,
            seed = _seedUsed,
            floor = _run.currentNode.floor,
            roomType = _run.currentNode.type.ToString(),
            wave = _currentWave != null ? _currentWave.name : "",
            character = PlayerBehaviour.instance.character.data.title,
        };
        _combatRecorder = new CombatRecorder(stats, () => Time.time, PlayerBehaviour.instance.character.mana);
        _entities.GetEntities(Entity.EntityType.Player).ForEach(x => _combatRecorder.AddUnit(x.GetComponent<Entity>()));
        _entities.GetEntities(Entity.EntityType.Computer).ForEach(x => _combatRecorder.AddUnit(x.GetComponent<Entity>()));
    }

    void OnEntitySummoned(Entity summon)
    {
        _combatRecorder?.AddUnit(summon);
    }

    void FinishCombatLog(bool won)
    {
        if (_combatRecorder == null)
        {
            return;
        }

        CombatStats stats = _combatRecorder.Stop(won);
        _combatRecorder = null;
        Debug.Log($"[CombatLog] {stats.ToSummary()}");
        try
        {
            CombatLogFile.Append(CombatLogFile.defaultPath, stats);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CombatLog] Could not write the combat log: {e.Message}");
        }
    }

    #endregion

    #region Rest

    // Heal every unit or resurrect a dead one
    void EnterRestRoom()
    {
        SetState(State.Rest);
        ShowRestChoices();
    }

    void ShowRestChoices()
    {
        GameObject character = PlayerBehaviour.instance.character.gameObject;
        string healDescription = GetRestHealDescription(GetRestHeal(character), GetRestManaRefill(character));
        ShowChoices("Rest", _restDescription, CreateRestChoices(healDescription, _run.deadAllies.Count > 0, () =>
        {
            HealAllies();
            RefillManaForRest(PlayerBehaviour.instance.character.mana);
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
            _resurrectedAllies.Add(ally);
        }
        LeaveRestRoom();
    }

    // An ally placed on the board leaves the roster; a resurrected one gets the ascension consumers the first time
    // it is placed since it came back
    void OnEntitySpawned(Entity entity)
    {
        if (entity == null || entity.entityType != Entity.EntityType.Player)
        {
            return;
        }

        player.roster.Remove(entity.data);
        if (ascension == null || !_resurrectedAllies.Remove(entity.data))
        {
            return;
        }

        foreach (AConsumerFactory consumer in ascension.GetResurrectConsumers(ascensionLevel))
        {
            ApplyConsumer(entity, consumer);
        }
    }

    void LeaveRestRoom()
    {
        if (_state == State.Rest)
        {
            SetState(State.ShowMap);
        }
    }

    // The RestHeal share of the max health of the character (items included), 30% by default
    public static float GetRestHeal(GameObject character)
    {
        return Mathf.Max(0f, GetAttribute(character, AttributeType.RestHeal));
    }

    // The RestManaRefill share of the max mana of the character (items included), 30% by default
    public static float GetRestManaRefill(GameObject character)
    {
        return Mathf.Max(0f, GetAttribute(character, AttributeType.RestManaRefill));
    }

    public static string GetRestHealDescription(float share, float manaShare)
    {
        return $"Every unit recovers {Mathf.RoundToInt(share * 100f)}% of its max health, and you recover {Mathf.RoundToInt(manaShare * 100f)}% of your max mana.";
    }

    // The RestManaRefill share of the max mana back on top of the mana left, capped by the max, like the refill
    // before a fight
    public static void RefillManaForRest(ResourceAttribute characterMana)
    {
        RefillMana(characterMana, GetRestManaRefill(characterMana.gameObject));
    }

    // That share of its max health back, through the regular resource flow so the heal feedbacks are triggered.
    // Never critical, and even on a unit that can't be healed for now
    public static void HealForRest(Entity entity, float share)
    {
        ResourceModifier heal = new ResourceModifier { source = entity.gameObject };
        heal.consumers.Add(new RuntimeConsumer(entity.health.Max * share, canBeCritical: false));
        entity.health.AddResourceModifier(heal);
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

            choices.Add(new EventChoice { label = ally.title, description = ally.description, source = ally, onSelected = () => resurrect(ally) });
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

    // The units left without a cell by a new board go back to the unit inventory, their items to the player
    void ReturnToInventory(List<GameObject> units)
    {
        foreach (GameObject unit in units)
        {
            Entity entity = unit.GetComponent<Entity>();
            if (entity.entityType != Entity.EntityType.Player)
            {
                EntityManager.instance.TakeBack(unit, entity.entityType);
                continue;
            }
            player.TakeItemsOf(entity);
            player.roster.Add(entity.data);
            EntityManager.instance.TakeBack(unit, entity.entityType);
        }
    }

    // The enemies spawned, none without a wave
    public List<Entity> LoadEnemies(WavePatternData waveData)
    {
        if (waveData == null)
        {
            return new List<Entity>();
        }

        // On the board of the wave
        Board board = EntityManager.instance.board;
        ReturnToInventory(board.SetLayout(waveData.layout));
        return EntityManager.instance.SpawnWave(waveData, board, Entity.EntityType.Computer);
    }

    #region IEventRoomHost

    public IReadOnlyList<EntityData> GetRewardEntities()
    {
        return DataManager.instance.GetRewardEntities(PlayerBehaviour.instance.character.data);
    }

    public System.Random random => _random;

    public void AddUnit(EntityData unit)
    {
        player.roster.Add(unit);
    }

    public IReadOnlyList<AItemFactory> GetItems(List<string> includedTags, List<string> excludedTags = null)
    {
        // Only the items for the class of the character
        return DataManager.instance.GetItems(includedTags, excludedTags, PlayerBehaviour.instance.character.data);
    }

    public void AddPlayerItem(AItem item)
    {
        player.character.inventoryHandler.AddItem(item, -1);
    }

    public void AddUnitItem(AItem item)
    {
        player.stash.AddItem(item, -1);
    }

    // Its screen is closed before the picked choice applies (see SelectEventChoice)
    public void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        _currentEvent = new EventPrompt(title, description, CloseBeforeEachChoice(choices, CloseEvent));
        screens.Open(GameScreen.Event);
    }

    public void EndEvent()
    {
        if (_state != State.PlayEvent)
        {
            return;
        }

        SetState(State.ShowMap);
    }

    #endregion

    // One of the choices of the event on screen, only the first one picked counts
    public void SelectEventChoice(EventChoice choice)
    {
        if (_currentEvent != null)
        {
            _currentEvent.Select(choice);
        }
    }

    void CloseEvent()
    {
        _currentEvent = null;
        screens.Close();
    }
}
