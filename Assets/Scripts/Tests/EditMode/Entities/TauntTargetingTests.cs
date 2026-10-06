using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// Taunt Totem: the attacks target the taunting entities first, except the ones picking at random
public class TauntTargetingTests
{
    readonly TestUnits _units = new TestUnits();
    GameplayTag _taunt;
    GameObject _first;
    GameObject _second;
    GameObject _taunting;

    [SetUp]
    public void SetUp()
    {
        _taunt = ScriptableObject.CreateInstance<GameplayTag>();
        _taunt.name = "Taunt";
        _first = _units.Create(100f, 100f, "First").gameObject;
        _second = _units.Create(100f, 100f, "Second").gameObject;
        Entity taunting = _units.Create(100f, 100f, "Taunting");
        taunting.AddTag(_taunt);
        _taunting = taunting.gameObject;
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_taunt);
    }

    List<GameObject> Prioritize(TargetBehaviourType type, GameplayTag tauntTag)
    {
        ATargetBehaviour targetBehaviour = ATargetBehaviour.Create(type);
        targetBehaviour.tauntTag = tauntTag;
        List<GameObject> targets = new List<GameObject> { _first, _taunting, _second };
        targetBehaviour.PrioritizeTaunting(targets);
        return targets;
    }

    [TestCase(TargetBehaviourType.Nearest)]
    [TestCase(TargetBehaviourType.LowestHealth)]
    [TestCase(TargetBehaviourType.Farest)]
    public void PrioritizeTaunting_MovesTheTauntingTargetFirstKeepingTheOthersOrder(TargetBehaviourType type)
    {
        CollectionAssert.AreEqual(new[] { _taunting, _first, _second }, Prioritize(type, _taunt));
    }

    [Test]
    public void PrioritizeTaunting_AtRandom_IgnoresTheTaunt()
    {
        CollectionAssert.AreEqual(new[] { _first, _taunting, _second }, Prioritize(TargetBehaviourType.Random, _taunt));
    }

    [Test]
    public void PrioritizeTaunting_WithoutTauntTag_ChangesNothing()
    {
        CollectionAssert.AreEqual(new[] { _first, _taunting, _second }, Prioritize(TargetBehaviourType.Nearest, null));
    }

    [Test]
    public void PrioritizeTaunting_SeveralTaunting_KeepTheirOrderFirst()
    {
        _second.GetComponent<Entity>().AddTag(_taunt);

        CollectionAssert.AreEqual(new[] { _taunting, _second, _first }, Prioritize(TargetBehaviourType.Nearest, _taunt));
    }
}

}
