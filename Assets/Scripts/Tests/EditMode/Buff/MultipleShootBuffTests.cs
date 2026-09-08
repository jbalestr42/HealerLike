using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class FakeTargetProvider : MonoBehaviour, ITargetProvider
{
    public int targetCount { get; set; }
    public TargetBehaviourType targetBehaviourType { get; set; }
    public List<GameObject> GetTargets() => new List<GameObject>();
}

public class MultipleShootBuffTests
{
    GameObject _source;
    GameObject _target;
    FakeTargetProvider _targetProvider;
    MultipleShootBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _targetProvider = _target.AddComponent<FakeTargetProvider>();
        _targetProvider.targetCount = 1;

        _buff = new MultipleShootBuff { data = new MultipleShootBuffData { value = 2 } };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void Add_IncreasesTargetCountByValue()
    {
        _buff.Add(_source, _target);

        Assert.AreEqual(3, _targetProvider.targetCount);
    }

    [Test]
    public void Remove_DecreasesTargetCountByValue()
    {
        _buff.Add(_source, _target);

        _buff.Remove(_source, _target);

        Assert.AreEqual(1, _targetProvider.targetCount);
    }

    [Test]
    public void Stack_IncreasesTargetCountByValue()
    {
        _buff.Add(_source, _target);

        _buff.Stack(_source, _target);

        Assert.AreEqual(5, _targetProvider.targetCount);
    }

    [Test]
    public void Unstack_DecreasesTargetCountByValue()
    {
        _buff.Add(_source, _target);
        _buff.Stack(_source, _target); // 5

        _buff.Unstack(_source, _target);

        Assert.AreEqual(3, _targetProvider.targetCount);
    }

    [Test]
    public void AddStackUnstackRemove_RoundTrip_ReturnsToOriginalCount()
    {
        _buff.Add(_source, _target);
        _buff.Stack(_source, _target);
        _buff.Unstack(_source, _target);
        _buff.Remove(_source, _target);

        Assert.AreEqual(1, _targetProvider.targetCount);
    }
}

}
