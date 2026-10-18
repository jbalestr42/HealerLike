using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The enemy marked by the Mark spell: attacked first by the units following the mark, the others keep their order
public class MarkTargetingTests
{
    readonly TestUnits _units = new TestUnits();
    GameplayTag _taunt;
    GameObject _first;
    GameObject _second;
    GameObject _marked;

    [SetUp]
    public void SetUp()
    {
        _taunt = ScriptableObject.CreateInstance<GameplayTag>();
        _taunt.name = "Taunt";
        _first = _units.Create(100f, 100f, "First").gameObject;
        _second = _units.Create(100f, 100f, "Second").gameObject;
        _marked = _units.Create(100f, 100f, "Marked").gameObject;
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_taunt);
    }

    List<GameObject> Prioritize(bool followsMark, GameObject marked, TargetBehaviourType type = TargetBehaviourType.Nearest)
    {
        ATargetBehaviour targetBehaviour = ATargetBehaviour.Create(type);
        targetBehaviour.followsMark = followsMark;
        targetBehaviour.tauntTag = _taunt;
        List<GameObject> targets = new List<GameObject> { _first, _second, _marked };
        targetBehaviour.PrioritizeTaunting(targets);
        targetBehaviour.PrioritizeMarked(targets, marked);
        return targets;
    }

    [TestCase(TargetBehaviourType.Nearest)]
    [TestCase(TargetBehaviourType.LowestHealth)]
    [TestCase(TargetBehaviourType.Random)]
    public void FollowingTheMark_TheMarkedFirst_TheOthersInTheirOrder(TargetBehaviourType type)
    {
        CollectionAssert.AreEqual(new[] { _marked, _first, _second }, Prioritize(true, _marked, type));
    }

    [Test]
    public void NotFollowingTheMark_ChangesNothing()
    {
        CollectionAssert.AreEqual(new[] { _first, _second, _marked }, Prioritize(false, _marked));
    }

    [Test]
    public void NoMarkedEnemy_ChangesNothing()
    {
        CollectionAssert.AreEqual(new[] { _first, _second, _marked }, Prioritize(true, null));
    }

    // Out of range or not an enemy of the unit: never added to its targets
    [Test]
    public void TheMarkedNotAmongTheTargets_NotAdded()
    {
        GameObject elsewhere = _units.Create(100f, 100f, "Elsewhere").gameObject;

        CollectionAssert.AreEqual(new[] { _first, _second, _marked }, Prioritize(true, elsewhere));
    }

    // The player chose it: before a taunting enemy too
    [Test]
    public void TheMarked_BeforeATauntingEnemy()
    {
        _second.GetComponent<Entity>().AddTag(_taunt);

        CollectionAssert.AreEqual(new[] { _marked, _second, _first }, Prioritize(true, _marked));
    }

    [Test]
    public void ANewBehaviour_DoesNotFollowTheMark_UntilTheUnitDoes()
    {
        Assert.IsFalse(ATargetBehaviour.Create(TargetBehaviourType.Nearest).followsMark);
    }
}

}
