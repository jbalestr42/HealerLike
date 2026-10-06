using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Map
{

// The map settings: one entry per room type, and the copy used to save the settings tuned in the map
// generation test scene into the source asset
public class MapGenerationSettingsTests
{
    MapGenerationSettings _source;
    MapGenerationSettings _target;

    [SetUp]
    public void SetUp()
    {
        _source = ScriptableObject.CreateInstance<MapGenerationSettings>();
        _source.name = "Source";
        _target = ScriptableObject.CreateInstance<MapGenerationSettings>();
        _target.name = "Target";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void Defaults_AreTheSlayTheSpireOnes()
    {
        RoomTypeSettings combat = _source.GetRoomType(MapNodeType.Combat);
        RoomTypeSettings elite = _source.GetRoomType(MapNodeType.Elite);
        RoomTypeSettings rest = _source.GetRoomType(MapNodeType.Rest);
        RoomTypeSettings treasure = _source.GetRoomType(MapNodeType.Treasure);

        // Combats on the first floor, treasures on the 9th, rests before the boss (15th)
        CollectionAssert.AreEqual(new[] { 0 }, combat.fixedFloors);
        CollectionAssert.IsEmpty(elite.fixedFloors);
        CollectionAssert.AreEqual(new[] { 14 }, rest.fixedFloors);
        CollectionAssert.AreEqual(new[] { 8 }, treasure.fixedFloors);
        // Neither elites nor rests below the 6th floor
        Assert.AreEqual(5, elite.firstFloor);
        Assert.AreEqual(5, rest.firstFloor);
        // Only combats can follow each other
        Assert.IsTrue(combat.canFollowItself);
        Assert.IsFalse(elite.canFollowItself || rest.canFollowItself || treasure.canFollowItself);
    }

    [Test]
    public void Defaults_EventRoomsListedButNeverDrawn()
    {
        RoomTypeSettings eventRoom = _source.GetRoomType(MapNodeType.Event);

        // Listed so its chance can be tuned, but no event to play yet
        Assert.IsNotNull(eventRoom);
        Assert.AreEqual(0f, eventRoom.weight);
        CollectionAssert.IsEmpty(eventRoom.fixedFloors);
        Assert.IsFalse(eventRoom.canFollowItself);
        CollectionAssert.IsEmpty(_source.eventRooms);
    }

    [Test]
    public void GetRoomType_MissingType_IsNull()
    {
        _source.roomTypes.RemoveAll(roomType => roomType.type == MapNodeType.Elite);

        Assert.IsNull(_source.GetRoomType(MapNodeType.Elite));
    }

    [Test]
    public void GetFixedType_IsTheTypeListingTheFloor()
    {
        Assert.AreEqual(MapNodeType.Combat, _source.GetFixedType(0));
        Assert.AreEqual(MapNodeType.Treasure, _source.GetFixedType(8));
        Assert.AreEqual(MapNodeType.Rest, _source.GetFixedType(14));
        Assert.IsNull(_source.GetFixedType(3));
    }

    [Test]
    public void GetFixedType_SeveralFixedFloors_AreAllFixed()
    {
        _source.GetRoomType(MapNodeType.Treasure).fixedFloors = new List<int> { 4, 8 };

        Assert.AreEqual(MapNodeType.Treasure, _source.GetFixedType(4));
        Assert.AreEqual(MapNodeType.Treasure, _source.GetFixedType(8));
    }

    [Test]
    public void GetFixedType_FloorListedTwice_IsTheFirstType()
    {
        _source.GetRoomType(MapNodeType.Elite).fixedFloors = new List<int> { 0 };

        Assert.AreEqual(MapNodeType.Combat, _source.GetFixedType(0));
    }

    [Test]
    public void CopyFrom_CopiesEverySetting()
    {
        _source.floorCount = 12;
        _source.columnCount = 5;
        _source.pathCount = 4;
        _source.startRoomCount = 2;
        _source.maxRoomsPerFloor = 3;
        RoomTypeSettings rest = _source.GetRoomType(MapNodeType.Rest);
        rest.fixedFloors = new List<int> { 6, 11 };
        rest.firstFloor = 4;
        rest.weight = 0.3f;
        rest.canFollowItself = true;

        _target.CopyFrom(_source);

        Assert.AreEqual(JsonUtility.ToJson(_source), JsonUtility.ToJson(_target));
        Assert.AreEqual(12, _target.floorCount);
        Assert.AreEqual(3, _target.maxRoomsPerFloor);
        RoomTypeSettings copiedRest = _target.GetRoomType(MapNodeType.Rest);
        CollectionAssert.AreEqual(new[] { 6, 11 }, copiedRest.fixedFloors);
        Assert.AreEqual(4, copiedRest.firstFloor);
        Assert.AreEqual(0.3f, copiedRest.weight, 0.0001f);
        Assert.IsTrue(copiedRest.canFollowItself);
    }

    [Test]
    public void CopyFrom_DoesNotShareTheRoomTypes()
    {
        _target.CopyFrom(_source);

        _source.GetRoomType(MapNodeType.Rest).weight = 0.9f;

        Assert.AreNotEqual(0.9f, _target.GetRoomType(MapNodeType.Rest).weight);
    }

    [Test]
    public void CopyFrom_KeepsTheName()
    {
        _target.CopyFrom(_source);

        Assert.AreEqual("Target", _target.name);
    }
}

}
