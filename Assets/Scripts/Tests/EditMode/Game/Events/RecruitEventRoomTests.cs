using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Events
{

public class RecruitEventRoomTests
{
    const string RecruitEventPath = "Assets/Data/Run/Events/RecruitEvent.asset";

    List<Object> _created = new List<Object>();
    FakeEventRoomHost _host;
    RecruitEventRoom _event;
    EntityData _knight;
    EntityData _archer;
    EntityData _mage;
    EntityData _beast;

    [SetUp]
    public void SetUp()
    {
        _knight = CreateUnit("Knight");
        _archer = CreateUnit("Archer");
        _mage = CreateUnit("Mage");
        _beast = CreateUnit("Beast");

        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        _created.Add(character);
        character.entities = new List<EntityData> { _knight, _archer, _knight };
        character.rewardEntities = new List<EntityData> { _mage, _beast };

        _host = new FakeEventRoomHost { characterData = character };
        _event = ScriptableObject.CreateInstance<RecruitEventRoom>();
        _created.Add(_event);
        _event.eventName = "Recruitment";
        _event.description = "Some fighters offer their help.";
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    EntityData CreateUnit(string title)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.title = title;
        unit.description = $"{title} description";
        _created.Add(unit);
        return unit;
    }

    [Test]
    public void PickUnits_DifferentUnitsUpToTheCount()
    {
        List<EntityData> pool = new List<EntityData> { _knight, _knight, _archer, null, _mage, _beast };

        for (int seed = 0; seed < 50; seed++)
        {
            List<EntityData> picked = RecruitEventRoom.PickUnits(pool, 3, new System.Random(seed));

            Assert.AreEqual(3, picked.Count);
            Assert.AreEqual(3, picked.Distinct().Count());
            Assert.IsFalse(picked.Contains(null));
        }
    }

    [Test]
    public void PickUnits_SmallPool_EveryUnitOnce()
    {
        List<EntityData> picked = RecruitEventRoom.PickUnits(new List<EntityData> { _knight, _knight, _archer }, 3, new System.Random(0));

        CollectionAssert.AreEquivalent(new[] { _knight, _archer }, picked);
    }

    [Test]
    public void PickUnits_EveryUnitCanBeOffered()
    {
        List<EntityData> pool = new List<EntityData> { _knight, _archer, _mage, _beast };
        HashSet<EntityData> offered = new HashSet<EntityData>();

        for (int seed = 0; seed < 50; seed++)
        {
            offered.UnionWith(RecruitEventRoom.PickUnits(pool, 1, new System.Random(seed)));
        }

        CollectionAssert.AreEquivalent(pool, offered);
    }

    [Test]
    public void Play_ShowsThreeUnitsOfTheCharacterThenLeave()
    {
        _event.Play(_host);

        Assert.AreEqual("Recruitment", _host.shownTitle);
        Assert.AreEqual("Some fighters offer their help.", _host.shownDescription);
        Assert.AreEqual(4, _host.shownChoices.Count);
        Assert.AreEqual("Leave", _host.shownChoices[3].label);
        string[] units = { "Knight", "Archer", "Mage", "Beast" };
        for (int i = 0; i < 3; i++)
        {
            CollectionAssert.Contains(units, _host.shownChoices[i].label);
        }
        Assert.AreEqual(3, _host.shownChoices.Take(3).Select(choice => choice.label).Distinct().Count());
    }

    [Test]
    public void Play_UnitChoiceShowsTheUnitDetails()
    {
        _event.Play(_host);

        EventChoice first = _host.shownChoices[0];
        EntityData unit = new[] { _knight, _archer, _mage, _beast }.First(candidate => candidate.title == first.label);
        Assert.AreEqual(CharacterCardText.GetUnitDetails(unit), first.description);
    }

    [Test]
    public void PickingAUnit_AddsItThenEndsTheEvent()
    {
        _event.Play(_host);
        string label = _host.shownChoices[1].label;

        _host.Pick(label);

        Assert.AreEqual(1, _host.addedUnits.Count);
        Assert.AreEqual(label, _host.addedUnits[0].title);
        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Leave_EndsTheEventWithoutAnyUnit()
    {
        _event.Play(_host);

        _host.Pick("Leave");

        CollectionAssert.IsEmpty(_host.addedUnits);
        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Play_CharacterWithoutUnits_OnlyLeave()
    {
        _host.characterData.entities = new List<EntityData>();
        _host.characterData.rewardEntities = new List<EntityData>();

        _event.Play(_host);

        Assert.AreEqual(1, _host.shownChoices.Count);
        Assert.AreEqual("Leave", _host.shownChoices[0].label);
    }

    [Test]
    public void RecruitEventAsset_OffersThreeUnitsAndIsInTheEventPool()
    {
        RecruitEventRoom asset = AssetDatabase.LoadAssetAtPath<RecruitEventRoom>(RecruitEventPath);
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");

        Assert.IsNotNull(asset, RecruitEventPath);
        Assert.AreEqual(3, asset.unitCount);
        Assert.IsNotEmpty(asset.eventName);
        Assert.IsNotEmpty(asset.description);
        EventRoomChance chance = settings.eventRooms.Find(eventRoom => eventRoom.eventRoom == asset);
        Assert.IsNotNull(chance);
        Assert.Greater(chance.weight, 0f);
    }
}

}
