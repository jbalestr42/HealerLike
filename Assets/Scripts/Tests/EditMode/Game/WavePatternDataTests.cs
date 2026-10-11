using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class WavePatternDataTests
{
    WavePatternData _wave;

    [SetUp]
    public void SetUp()
    {
        _wave = ScriptableObject.CreateInstance<WavePatternData>();
        _wave.width = 3;
        _wave.height = 2;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_wave);
    }

    [Test]
    public void GetSlotPosition_FirstSlot_IsOffsetByHalfThePatternSize()
    {
        Vector3 center = new Vector3(5f, 0.5f, 0f);

        Vector3 position = _wave.GetSlotPosition(center, 0, 0);

        Assert.AreEqual(new Vector3(3.5f, 0.5f, -1f), position);
    }

    [Test]
    public void GetSlotPosition_NextSlots_AreOneUnitApartOnXAndZ()
    {
        Vector3 origin = _wave.GetSlotPosition(Vector3.zero, 0, 0);

        Assert.AreEqual(origin + new Vector3(2f, 0f, 1f), _wave.GetSlotPosition(Vector3.zero, 2, 1));
    }

    [Test]
    public void GetSlotPosition_KeepsTheCenterHeight()
    {
        Assert.AreEqual(2f, _wave.GetSlotPosition(new Vector3(0f, 2f, 0f), 1, 1).y);
    }

    EntityData CreateUnit(string name)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.name = name;
        _units.Add(unit);
        return unit;
    }

    readonly System.Collections.Generic.List<EntityData> _units = new System.Collections.Generic.List<EntityData>();

    [TearDown]
    public void DestroyUnits()
    {
        foreach (EntityData unit in _units)
        {
            Object.DestroyImmediate(unit);
        }
        _units.Clear();
    }

    [Test]
    public void SetEntity_ThenGetEntity_TheUnitOfTheCell_OthersEmpty()
    {
        EntityData soldier = CreateUnit("Soldier");

        _wave.SetEntity(2, 1, soldier);

        Assert.AreSame(soldier, _wave.GetEntity(2, 1));
        Assert.IsNull(_wave.GetEntity(1, 2), "another cell");
        Assert.IsNull(_wave.GetEntity(0, 0));
    }

    [Test]
    public void OutOfTheGrid_NoUnit_AndNothingSet()
    {
        EntityData soldier = CreateUnit("Soldier");

        _wave.SetEntity(3, 0, soldier);
        _wave.SetEntity(0, -1, soldier);

        Assert.IsNull(_wave.GetEntity(3, 0));
        Assert.IsNull(_wave.GetEntity(-1, 0));
        CollectionAssert.IsEmpty(_wave.GetUnits());
    }

    [Test]
    public void GetUnits_ColumnAfterColumn_EmptyCellsLeftOut()
    {
        EntityData front = CreateUnit("Front");
        EntityData back = CreateUnit("Back");
        EntityData middle = CreateUnit("Middle");
        _wave.SetEntity(2, 0, back);
        _wave.SetEntity(0, 1, front);
        _wave.SetEntity(1, 0, middle);

        CollectionAssert.AreEqual(new[] { (0, 1, front), (1, 0, middle), (2, 0, back) }, _wave.GetUnits());
    }

    [Test]
    public void Resize_KeepsTheUnitsOfTheCellsStillInTheGrid()
    {
        EntityData kept = CreateUnit("Kept");
        EntityData cut = CreateUnit("Cut");
        _wave.SetEntity(1, 1, kept);
        _wave.SetEntity(2, 0, cut);

        _wave.Resize(2, 4);

        Assert.AreEqual(2, _wave.width);
        Assert.AreEqual(4, _wave.height);
        Assert.AreSame(kept, _wave.GetEntity(1, 1));
        Assert.IsNull(_wave.GetEntity(1, 3), "new cell");
        CollectionAssert.AreEqual(new[] { (1, 1, kept) }, _wave.GetUnits());
    }

    [Test]
    public void SettingTheWidthOrTheHeight_ResizesTheGrid()
    {
        EntityData unit = CreateUnit("Unit");
        _wave.SetEntity(0, 1, unit);

        _wave.width = 1;
        _wave.height = 3;

        Assert.AreSame(unit, _wave.GetEntity(0, 1));
        _wave.SetEntity(0, 2, unit);
        Assert.AreSame(unit, _wave.GetEntity(0, 2));
    }
}

}
