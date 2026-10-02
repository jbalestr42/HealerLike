using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Events
{

// Prepared without content yet: the player can only leave, and it's never drawn
public class CursedTreasureEventRoomTests
{
    const string CursedTreasureEventPath = "Assets/Data/Run/Events/CursedTreasureEvent.asset";

    CursedTreasureEventRoom _event;
    FakeEventRoomHost _host;

    [SetUp]
    public void SetUp()
    {
        _event = ScriptableObject.CreateInstance<CursedTreasureEventRoom>();
        _event.eventName = "Cursed Treasure";
        _event.description = "A chest covered in dark runes.";
        _host = new FakeEventRoomHost();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_event);
    }

    [Test]
    public void Play_ShowsTheEventWithOnlyLeave()
    {
        _event.Play(_host);

        Assert.AreEqual("Cursed Treasure", _host.shownTitle);
        Assert.AreEqual("A chest covered in dark runes.", _host.shownDescription);
        Assert.AreEqual(1, _host.shownChoices.Count);
        Assert.AreEqual("Leave", _host.shownChoices[0].label);
        Assert.AreEqual(0, _host.endCount);
    }

    [Test]
    public void Leave_EndsTheEvent()
    {
        _event.Play(_host);

        _host.Pick("Leave");

        Assert.AreEqual(1, _host.endCount);
        CollectionAssert.IsEmpty(_host.addedUnits);
    }

    [Test]
    public void CursedTreasureAsset_ListedButNeverDrawn()
    {
        CursedTreasureEventRoom asset = AssetDatabase.LoadAssetAtPath<CursedTreasureEventRoom>(CursedTreasureEventPath);
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");

        Assert.IsNotNull(asset, CursedTreasureEventPath);
        Assert.IsNotEmpty(asset.eventName);
        Assert.IsNotEmpty(asset.description);
        EventRoomChance chance = settings.eventRooms.Find(eventRoom => eventRoom.eventRoom == asset);
        Assert.IsNotNull(chance);
        Assert.AreEqual(0f, chance.weight);
    }
}

}
