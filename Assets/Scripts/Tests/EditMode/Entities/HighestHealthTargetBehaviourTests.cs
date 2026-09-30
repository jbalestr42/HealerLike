using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// Targets the unit with the most current health first: the tanks and the units at full health
public class HighestHealthTargetBehaviourTests
{
    TestUnits _units;
    HighestHealthTargetBehaviour _behaviour;

    [SetUp]
    public void SetUp()
    {
        _units = new TestUnits();
        _behaviour = new HighestHealthTargetBehaviour();
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void ApplyBehaviour_SortsFromTheMostToTheLeastCurrentHealth()
    {
        GameObject low = _units.Create(20f, 100f, "Low").gameObject;
        GameObject high = _units.Create(90f, 100f, "High").gameObject;
        GameObject middle = _units.Create(50f, 100f, "Middle").gameObject;
        List<GameObject> targets = new List<GameObject> { low, high, middle };

        _behaviour.ApplyBehaviour(targets, Vector3.zero, 10f);

        CollectionAssert.AreEqual(new List<GameObject> { high, middle, low }, targets);
    }

    [Test]
    public void ApplyBehaviour_ComparesTheCurrentHealth_NotThePercent()
    {
        // A wounded tank still has more health than a small unit at full health
        GameObject tank = _units.Create(150f, 300f, "Tank").gameObject;
        GameObject small = _units.Create(60f, 60f, "Small").gameObject;
        List<GameObject> targets = new List<GameObject> { small, tank };

        _behaviour.ApplyBehaviour(targets, Vector3.zero, 10f);

        CollectionAssert.AreEqual(new List<GameObject> { tank, small }, targets);
    }
}

}
