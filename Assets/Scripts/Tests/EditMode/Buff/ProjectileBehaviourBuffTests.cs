using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class FakeProjectileBehaviour : AProjectileBehaviour
{
    public override void Init(GameObject source) { }
}

public class FakeStackableProjectileBehaviour : AProjectileBehaviour, IStackableBuff
{
    public int stackCount;
    public int unstackCount;

    public override void Init(GameObject source) { }
    public void Stack(GameObject source, GameObject target) => stackCount++;
    public void Unstack(GameObject source, GameObject target) => unstackCount++;
}

public class FakeProjectileBehaviourFactory : AProjectileBehaviourFactory
{
    public override AProjectileBehaviour AddBehaviour(GameObject target) => target.AddComponent<FakeProjectileBehaviour>();
}

public class FakeStackableProjectileBehaviourFactory : AProjectileBehaviourFactory
{
    public override AProjectileBehaviour AddBehaviour(GameObject target) => target.AddComponent<FakeStackableProjectileBehaviour>();
}

public class ProjectileBehaviourBuffTests
{
    GameObject _source;
    GameObject _target;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    ProjectileBehaviourBuff CreateBuff(AProjectileBehaviourFactory factory)
    {
        return new ProjectileBehaviourBuff { data = new ProjectileBehaviourBuffData { projectileBehaviour = factory } };
    }

    [Test]
    public void Add_AddsProjectileBehaviourInstanceToTarget()
    {
        ProjectileBehaviourBuff buff = CreateBuff(CreateTracked<FakeProjectileBehaviourFactory>());

        buff.Add(_source, _target);

        Assert.IsNotNull(_target.GetComponent<FakeProjectileBehaviour>());
    }

    [Test]
    public void IsStackable_NonStackableBehaviour_ReturnsFalse()
    {
        ProjectileBehaviourBuff buff = CreateBuff(CreateTracked<FakeProjectileBehaviourFactory>());
        buff.Add(_source, _target);

        Assert.IsFalse(buff.isStackable);
    }

    [Test]
    public void IsStackable_StackableBehaviour_ReturnsTrue()
    {
        ProjectileBehaviourBuff buff = CreateBuff(CreateTracked<FakeStackableProjectileBehaviourFactory>());
        buff.Add(_source, _target);

        Assert.IsTrue(buff.isStackable);
    }

    [Test]
    public void Stack_DelegatesToProjectileBehaviourInstance()
    {
        ProjectileBehaviourBuff buff = CreateBuff(CreateTracked<FakeStackableProjectileBehaviourFactory>());
        buff.Add(_source, _target);
        FakeStackableProjectileBehaviour behaviour = _target.GetComponent<FakeStackableProjectileBehaviour>();

        buff.Stack(_source, _target);

        Assert.AreEqual(1, behaviour.stackCount);
    }

    [Test]
    public void Unstack_DelegatesToProjectileBehaviourInstance()
    {
        ProjectileBehaviourBuff buff = CreateBuff(CreateTracked<FakeStackableProjectileBehaviourFactory>());
        buff.Add(_source, _target);
        FakeStackableProjectileBehaviour behaviour = _target.GetComponent<FakeStackableProjectileBehaviour>();

        buff.Unstack(_source, _target);

        Assert.AreEqual(1, behaviour.unstackCount);
    }
}

}
