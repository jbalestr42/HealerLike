using NUnit.Framework;
using UnityEngine;

namespace Players
{

// The units the player can place on the board, a copy per unit
public class UnitRosterTests
{
    EntityData _zealot;
    EntityData _archer;

    [SetUp]
    public void SetUp()
    {
        _zealot = ScriptableObject.CreateInstance<EntityData>();
        _archer = ScriptableObject.CreateInstance<EntityData>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_zealot);
        Object.DestroyImmediate(_archer);
    }

    [Test]
    public void Init_ListsEveryCopy()
    {
        UnitRoster roster = new UnitRoster();

        roster.Init(new[] { _zealot, _zealot, _archer });

        CollectionAssert.AreEqual(new[] { _zealot, _zealot, _archer }, roster.units);
    }

    [Test]
    public void Remove_TakesOneCopyOnly()
    {
        UnitRoster roster = new UnitRoster();
        roster.Init(new[] { _zealot, _zealot });

        Assert.IsTrue(roster.Remove(_zealot));

        CollectionAssert.AreEqual(new[] { _zealot }, roster.units);
    }

    [Test]
    public void Remove_UnitNotInTheRoster_ChangesNothing()
    {
        UnitRoster roster = new UnitRoster();
        roster.Init(new[] { _zealot });
        int changes = 0;
        roster.OnChanged.AddListener(() => changes++);

        Assert.IsFalse(roster.Remove(_archer));

        Assert.AreEqual(0, changes);
        CollectionAssert.AreEqual(new[] { _zealot }, roster.units);
    }

    [Test]
    public void Add_IsListedLastAndTold()
    {
        UnitRoster roster = new UnitRoster();
        roster.Init(new[] { _zealot });
        int changes = 0;
        roster.OnChanged.AddListener(() => changes++);

        roster.Add(_archer);

        CollectionAssert.AreEqual(new[] { _zealot, _archer }, roster.units);
        Assert.AreEqual(1, changes);
    }
}

}
