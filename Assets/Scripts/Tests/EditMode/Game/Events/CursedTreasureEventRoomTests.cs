using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Events
{

// A chest holding one cursed unit item, revealed once opened, or left closed
public class CursedTreasureEventRoomTests
{
    const string CursedTreasureEventPath = "Assets/Data/Run/Events/CursedTreasureEvent.asset";

    readonly List<Object> _created = new List<Object>();
    CursedTreasureEventRoom _event;
    FakeEventRoomHost _host;
    GameplayTag _entityTag;
    GameplayTag _playerTag;
    GameplayTag _cursedTag;
    List<AItemFactory> _cursedUnitItems;

    [SetUp]
    public void SetUp()
    {
        _entityTag = CreateTag(TagNames.Entity);
        _playerTag = CreateTag(TagNames.Player);
        _cursedTag = CreateTag(TagNames.Cursed);
        _cursedUnitItems = new List<AItemFactory>
        {
            CreateItem("Bloodthirst Blade", "+60% damage", _entityTag, _cursedTag),
            CreateItem("Thorned Crown", "+6 flat armor", _entityTag, _cursedTag),
        };

        _event = ScriptableObject.CreateInstance<CursedTreasureEventRoom>();
        _created.Add(_event);
        _event.eventName = "Cursed Treasure";
        _event.description = "A chest covered in dark runes.";

        _host = new FakeEventRoomHost();
        // Neither a regular unit item nor a cursed player item is in the chest
        _host.items.Add(CreateItem("Whetstone", "+5 damage", _entityTag));
        _host.items.Add(CreateItem("Hasty Grimoire", "-30% cooldown", _playerTag, _cursedTag));
        _host.items.AddRange(_cursedUnitItems);
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

    GameplayTag CreateTag(string name)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        _created.Add(tag);
        return tag;
    }

    ItemFactory CreateItem(string title, string description, params GameplayTag[] tags)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { name = title, description = description, tags = new List<GameplayTag>(tags) };
        _created.Add(item);
        return item;
    }

    List<string> ShownLabels()
    {
        return _host.shownChoices.Select(choice => choice.label).ToList();
    }

    [Test]
    public void ItemTags_TheCursedUnitItems()
    {
        CollectionAssert.AreEquivalent(new[] { TagNames.Entity, TagNames.Cursed }, CursedTreasureEventRoom.ItemTags);
    }

    [Test]
    public void Play_OpenOrLeave()
    {
        _event.Play(_host);

        Assert.AreEqual("Cursed Treasure", _host.shownTitle);
        Assert.AreEqual("A chest covered in dark runes.", _host.shownDescription);
        CollectionAssert.AreEqual(new[] { "Open", "Leave" }, ShownLabels());
        Assert.AreEqual(0, _host.endCount);
    }

    [Test]
    public void Open_GivesACursedUnitItemAndRevealsIt()
    {
        _event.Play(_host);

        _host.Pick("Open");

        Assert.AreEqual(1, _host.addedUnitItems.Count);
        AItem item = _host.addedUnitItems[0];
        CollectionAssert.Contains(_cursedUnitItems.Select(factory => factory.title).ToList(), item.title);
        CollectionAssert.IsEmpty(_host.addedItems);
        StringAssert.Contains(item.title, _host.shownDescription);
        CollectionAssert.AreEqual(new[] { "Continue" }, ShownLabels());
        Assert.AreEqual(0, _host.endCount);
    }

    [Test]
    public void Continue_AfterOpening_EndsTheEvent()
    {
        _event.Play(_host);
        _host.Pick("Open");

        _host.Pick("Continue");

        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Leave_EndsTheEventWithoutAnyItem()
    {
        _event.Play(_host);

        _host.Pick("Leave");

        Assert.AreEqual(1, _host.endCount);
        CollectionAssert.IsEmpty(_host.addedUnitItems);
    }

    [Test]
    public void Open_EveryCursedUnitItemCanBeInTheChest()
    {
        HashSet<string> found = new HashSet<string>();
        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);
            _host.addedUnitItems.Clear();
            _event.Play(_host);
            _host.Pick("Open");
            found.Add(_host.addedUnitItems[0].title);
        }

        CollectionAssert.AreEquivalent(_cursedUnitItems.Select(factory => factory.title), found);
    }

    [Test]
    public void Play_NoCursedUnitItem_OnlyLeave()
    {
        _host.items.RemoveAll(item => _cursedUnitItems.Contains(item));

        _event.Play(_host);

        CollectionAssert.AreEqual(new[] { "Leave" }, ShownLabels());
    }

    [Test]
    public void GetRevealText_TheItemThenItsDescription()
    {
        AItem item = _cursedUnitItems[0].GetItem();

        Assert.AreEqual("You found <b>Bloodthirst Blade</b>.\n+60% damage", CursedTreasureEventRoom.GetRevealText(item));
    }

    [Test]
    public void CursedTreasureAsset_IsInTheEventPool()
    {
        CursedTreasureEventRoom asset = AssetDatabase.LoadAssetAtPath<CursedTreasureEventRoom>(CursedTreasureEventPath);
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");

        Assert.IsNotNull(asset, CursedTreasureEventPath);
        Assert.IsNotEmpty(asset.eventName);
        Assert.IsNotEmpty(asset.description);
        EventRoomChance chance = settings.eventRooms.Find(eventRoom => eventRoom.eventRoom == asset);
        Assert.IsNotNull(chance);
        Assert.Greater(chance.weight, 0f);
    }
}

}
