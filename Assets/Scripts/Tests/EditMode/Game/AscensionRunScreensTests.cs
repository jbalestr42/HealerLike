using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game
{

// What the screens of a run ask of it: start the battle, pick a room, a reward or a choice, look at the map. The run
// is put in a state by hand (not started), without the singletons
public class AscensionRunScreensTests
{
    GameObject _go;
    GameObject _characterGo;
    GameManager _game;
    PlayerBehaviour _player;
    AscensionGameType _ascension;
    readonly List<Object> _objects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Run");
        _game = _go.AddComponent<GameManager>();
        _player = _go.AddComponent<PlayerBehaviour>();
        _ascension = _go.AddComponent<AscensionGameType>();
        TestHelpers.SetPrivateField(_ascension, "_game", _game);
        TestHelpers.SetPrivateField(_ascension, "_player", _player);
        _characterGo = new GameObject("Character");
        TestHelpers.WithLoggingDisabled(() => _player.character = _characterGo.AddComponent<Character>());
        _game.screens.Open(GameScreen.Game);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_characterGo);
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    void SetState(AscensionGameType.State state)
    {
        TestHelpers.SetPrivateField(_ascension, "_state", state);
    }

    EntityData CreateUnit()
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        _objects.Add(unit);
        return unit;
    }

    static Item CreateItem(string name)
    {
        return new Item { data = new ItemData { name = name, description = "" } };
    }

    // A room on the first floor, then the boss
    RunState CreateRun()
    {
        MapNode room = new MapNode(0, 0, MapNodeType.Combat);
        MapNode boss = new MapNode(1, 0, MapNodeType.Boss);
        room.Connect(boss);
        RunState run = new RunState(new RunMap(new List<List<MapNode>> { new List<MapNode> { room } }, boss, 1));
        TestHelpers.SetPrivateField(_ascension, "_run", run);
        return run;
    }

    void ShowRewards(params RewardChoice[] choices)
    {
        SetState(AscensionGameType.State.SelectUpgrade);
        TestHelpers.SetPrivateField(_ascension, "_rewardChoices", new List<RewardChoice>(choices));
        _game.screens.Open(GameScreen.Reward);
    }

    [Test]
    public void StartBattle_WhilePreparing_StartsIt()
    {
        SetState(AscensionGameType.State.WaitForRoundToStart);

        _ascension.StartBattle();

        Assert.AreEqual(AscensionGameType.State.StartBattle, _ascension.state);
    }

    [Test]
    public void StartBattle_OutsideThePreparation_DoesNothing()
    {
        SetState(AscensionGameType.State.OnGoingBattle);

        _ascension.StartBattle();

        Assert.AreEqual(AscensionGameType.State.OnGoingBattle, _ascension.state);
        Assert.IsFalse(_ascension.canStartBattle);
    }

    [Test]
    public void LookAtMap_WhilePreparing_OpensTheMapWithoutAnyRoomToPick()
    {
        CreateRun();
        SetState(AscensionGameType.State.WaitForRoundToStart);

        _ascension.LookAtMap();

        Assert.AreEqual(GameScreen.Map, _game.screens.current);
        Assert.IsFalse(_ascension.canSelectRoom);
        Assert.IsFalse(_ascension.canLookAtMap, "Already open");
    }

    [Test]
    public void LookAtMap_InBattle_DoesNothing()
    {
        SetState(AscensionGameType.State.OnGoingBattle);

        _ascension.LookAtMap();

        Assert.AreEqual(GameScreen.Game, _game.screens.current);
    }

    [Test]
    public void CloseMap_LookedAt_BackToTheUnits()
    {
        SetState(AscensionGameType.State.WaitForRoundToStart);
        _ascension.LookAtMap();

        _ascension.CloseMap();

        Assert.AreEqual(GameScreen.Game, _game.screens.current);
    }

    [Test]
    public void CloseMap_RoomToPick_StaysOpen()
    {
        SetState(AscensionGameType.State.SelectRoom);
        _game.screens.Open(GameScreen.Map);

        _ascension.CloseMap();

        Assert.AreEqual(GameScreen.Map, _game.screens.current);
    }

    [Test]
    public void SelectRoom_NextRoom_TravelsClosesTheMapAndEntersItNext()
    {
        RunState run = CreateRun();
        SetState(AscensionGameType.State.SelectRoom);
        _game.screens.Open(GameScreen.Map);
        List<MapNode> selected = new List<MapNode>();
        UnityEngine.Events.UnityAction<MapNode> record = selected.Add;
        AscensionGameType.OnRoomSelected.AddListener(record);
        try
        {
            MapNode room = run.GetAvailableNodes()[0];

            _ascension.SelectRoom(room);

            Assert.AreSame(room, run.currentNode);
            Assert.AreEqual(GameScreen.Game, _game.screens.current);
            Assert.AreEqual(AscensionGameType.State.EnterRoom, _ascension.state);
            CollectionAssert.AreEqual(new[] { room }, selected);
        }
        finally
        {
            AscensionGameType.OnRoomSelected.RemoveListener(record);
        }
    }

    [Test]
    public void SelectRoom_NotTheNextOne_DoesNothing()
    {
        RunState run = CreateRun();
        SetState(AscensionGameType.State.SelectRoom);
        _game.screens.Open(GameScreen.Map);

        _ascension.SelectRoom(run.map.boss);

        Assert.IsNull(run.currentNode);
        Assert.AreEqual(GameScreen.Map, _game.screens.current);
    }

    [Test]
    public void SelectReward_Unit_JoinsTheRosterAndBackToTheMap()
    {
        EntityData unit = CreateUnit();
        RewardChoice choice = new RewardChoice { type = RewardChoiceType.Unit, unit = unit };
        ShowRewards(choice, new RewardChoice { type = RewardChoiceType.EntityItem, item = CreateItem("Ward") });

        _ascension.SelectReward(choice);

        CollectionAssert.AreEqual(new[] { unit }, _player.roster.units);
        Assert.IsEmpty(_ascension.rewardChoices);
        Assert.AreEqual(GameScreen.Game, _game.screens.current);
        Assert.AreEqual(AscensionGameType.State.ShowMap, _ascension.state);
    }

    [Test]
    public void SelectReward_PartyEquipment_GoesToTheStash()
    {
        RewardChoice choice = new RewardChoice { type = RewardChoiceType.EntityItem, item = CreateItem("Ward") };
        ShowRewards(choice);

        _ascension.SelectReward(choice);

        CollectionAssert.AreEqual(new[] { choice.item }, _player.stash.items.Select(entry => entry.item).ToList());
    }

    [Test]
    public void SelectReward_HealerUpgrade_GoesToTheCharacter()
    {
        RewardChoice choice = new RewardChoice { type = RewardChoiceType.PlayerItem, item = CreateItem("Bloom") };
        ShowRewards(choice);

        _ascension.SelectReward(choice);

        CollectionAssert.AreEqual(new[] { choice.item },
            _player.character.inventoryHandler.items.Select(entry => entry.item).ToList());
        Assert.IsEmpty(_player.stash.items);
    }

    [Test]
    public void SelectReward_NotOnScreen_DoesNothing()
    {
        ShowRewards(new RewardChoice { type = RewardChoiceType.EntityItem, item = CreateItem("Ward") });

        _ascension.SelectReward(new RewardChoice { type = RewardChoiceType.EntityItem, item = CreateItem("Bloom") });

        Assert.IsEmpty(_player.stash.items);
        Assert.AreEqual(GameScreen.Reward, _game.screens.current);
    }

    [Test]
    public void ShowChoices_OpensTheEventScreen()
    {
        _ascension.ShowChoices("Rest", "Take a break", new[] { new EventChoice { label = "Heal" } });

        Assert.AreEqual(GameScreen.Event, _game.screens.current);
        Assert.AreEqual("Rest", _ascension.currentEvent.title);
        Assert.AreEqual("Heal", _ascension.currentEvent.choices[0].label);
    }

    [Test]
    public void SelectEventChoice_ClosesTheScreenThenAppliesTheChoice()
    {
        GameScreen screenWhenApplied = GameScreen.None;
        EventChoice heal = new EventChoice { label = "Heal", onSelected = () => screenWhenApplied = _game.screens.current };
        _ascension.ShowChoices("Rest", "", new[] { heal });

        _ascension.SelectEventChoice(_ascension.currentEvent.choices[0]);

        Assert.AreEqual(GameScreen.Game, screenWhenApplied);
        Assert.IsNull(_ascension.currentEvent);
    }

    [Test]
    public void SelectEventChoice_OpeningTheNextChoices_ShowsThem()
    {
        EventChoice resurrect = new EventChoice
        {
            label = "Resurrect",
            onSelected = () => _ascension.ShowChoices("Resurrect", "", new[] { new EventChoice { label = "Back" } }),
        };
        _ascension.ShowChoices("Rest", "", new[] { resurrect });

        _ascension.SelectEventChoice(_ascension.currentEvent.choices[0]);

        Assert.AreEqual(GameScreen.Event, _game.screens.current);
        Assert.AreEqual("Resurrect", _ascension.currentEvent.title);
    }

    [Test]
    public void ShowChoices_ChoiceGivingAUnit_KeepsItsSource()
    {
        EntityData unit = CreateUnit();

        _ascension.ShowChoices("Recruit", "", new[] { new EventChoice { label = "Zealot", source = unit } });

        Assert.AreSame(unit, _ascension.currentEvent.choices[0].source);
    }
}

}
