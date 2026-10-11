using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

public class TargetProviderTests
{
    GameObject _go;
    TargetProvider _targetProvider;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TargetProvider");
        // AddComponent calls the editor Reset() message, which fails before Init()
        TestHelpers.WithLoggingDisabled(() => _targetProvider = _go.AddComponent<TargetProvider>());
        TestHelpers.SetPrivateField(_targetProvider, "_targetBehaviour", ATargetBehaviour.Create(TargetBehaviourType.Nearest));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    ATargetBehaviour GetTargetBehaviour()
    {
        return TestHelpers.GetPrivateField<ATargetBehaviour>(_targetProvider, "_targetBehaviour");
    }

    [Test]
    public void SetTargetBehaviour_ChangesTheType()
    {
        _targetProvider.targetBehaviourType = TargetBehaviourType.LowestHealth;

        Assert.AreEqual(TargetBehaviourType.LowestHealth, _targetProvider.targetBehaviourType);
    }

    [Test]
    public void SetTargetBehaviour_KeepsTheTargetCount()
    {
        _targetProvider.targetCount = 3;

        _targetProvider.targetBehaviourType = TargetBehaviourType.Random;

        Assert.AreEqual(3, _targetProvider.targetCount);
    }

    [Test]
    public void SetTargetBehaviour_KeepsTheTargetValidators()
    {
        List<ATargetValidator> validators = GetTargetBehaviour().targetValidators;

        _targetProvider.targetBehaviourType = TargetBehaviourType.Farest;

        Assert.AreSame(validators, GetTargetBehaviour().targetValidators);
    }

    [Test]
    public void GetTargets_LeavesOutTheUnitsDestroyedSinceTheyWerePicked()
    {
        GameObject alive = new GameObject("Alive");
        GameObject killed = new GameObject("Killed");
        TestHelpers.SetPrivateField(_targetProvider, "_targets", new List<GameObject> { killed, alive });
        Object.DestroyImmediate(killed);

        List<GameObject> targets = _targetProvider.GetTargets();

        Assert.AreEqual(1, targets.Count);
        Assert.AreSame(alive, targets[0]);
        Object.DestroyImmediate(alive);
    }
}

}
