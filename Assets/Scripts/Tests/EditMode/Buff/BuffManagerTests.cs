using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class FakeBuffData
{
    public List<string> log = new List<string>();
}

public class FakeBuff : ABuff<FakeBuffData>
{
    public override void Instant(GameObject source, GameObject target) => data.log.Add("Instant");
    public override void Add(GameObject source, GameObject target) => data.log.Add("Add");
    public override void Remove(GameObject source, GameObject target) => data.log.Add("Remove");
}

public class FakeStackableBuff : ABuff<FakeBuffData>, IStackableBuff
{
    public override void Instant(GameObject source, GameObject target) => data.log.Add("Instant");
    public override void Add(GameObject source, GameObject target) => data.log.Add("Add");
    public override void Remove(GameObject source, GameObject target) => data.log.Add("Remove");
    public void Stack(GameObject source, GameObject target) => data.log.Add("Stack");
    public void Unstack(GameObject source, GameObject target) => data.log.Add("Unstack");
}

public class HandlerRecordingBuff : ABuff<List<ABuffHandler>>
{
    public override void Instant(GameObject source, GameObject target) => data.Add(buffHandler);
    public override void Add(GameObject source, GameObject target) => data.Add(buffHandler);
    public override void Remove(GameObject source, GameObject target) { }
}

public class FakeBuffFactory : BuffFactory<FakeBuff, FakeBuffData> { }
public class HandlerRecordingBuffFactory : BuffFactory<HandlerRecordingBuff, List<ABuffHandler>> { }
public class FakeStackableBuffFactory : BuffFactory<FakeStackableBuff, FakeBuffData> { }

public class BuffManagerTests
{
    GameObject _source;
    GameObject _target;
    BuffManager _buffManager;
    FakeBuffData _data;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _buffManager = new GameObject("BuffManager").AddComponent<BuffManager>();
        _buffManager.isEnabled = true;
        _data = new FakeBuffData();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_buffManager.gameObject);
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

    ABuffHandlerFactory CreateHandlerFactory(ABuffFactory buffFactory, DurationType durationType, float duration = 100f, List<GameplayTag> tags = null)
    {
        BuffHandlerFactory handlerFactory = CreateTracked<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = durationType,
            duration = duration,
            buffFactoryList = new List<ABuffFactory> { buffFactory },
            tags = tags ?? new List<GameplayTag>(),
        };
        return handlerFactory;
    }

    [TestCase(DurationType.Instant)]
    [TestCase(DurationType.Infinite)]
    public void AddHandler_GivesTheBuffItsOwningHandler(DurationType durationType)
    {
        HandlerRecordingBuffFactory buffFactory = CreateTracked<HandlerRecordingBuffFactory>();
        buffFactory.data = new List<ABuffHandler>();
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, durationType);
        ABuffHandler handler = null;
        _buffManager.OnBuffHandlerStarted.AddListener(buffHandlerData => handler = buffHandlerData.buffHandler);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(1, buffFactory.data.Count);
        Assert.IsNotNull(buffFactory.data[0]);
        if (durationType != DurationType.Instant)
        {
            Assert.AreSame(handler, buffFactory.data[0]);
        }
    }

    [Test]
    public void AddHandler_InstantDuration_InvokesInstantOncePerRefreshStack()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Instant);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Instant", "Instant" }, _data.log);
    }

    [Test]
    public void AddHandler_DurationType_StartsHandlerAndAddsBuff()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        int startedCount = 0;
        _buffManager.OnBuffHandlerStarted.AddListener(_ => startedCount++);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
        Assert.AreEqual(1, startedCount);
    }

    [Test]
    public void AddHandler_RecordsTheSourceOnTheHandlerData()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        GameObject startedSource = null;
        _buffManager.OnBuffHandlerStarted.AddListener(buffHandlerData => startedSource = buffHandlerData.source);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreSame(_source, startedSource);
    }

    [Test]
    public void RemoveHandler_NonStackableBuff_RemovesBuffAndStopsHandler()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        int stoppedCount = 0;
        _buffManager.OnBuffHandlerStopped.AddListener(_ => stoppedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Remove + Stop

        CollectionAssert.AreEqual(new[] { "Add", "Remove" }, _data.log);
        Assert.AreEqual(1, stoppedCount);
    }

    [Test]
    public void AddHandler_CalledTwiceBeforeUpdate_StacksSecondApplication()
    {
        FakeStackableBuffFactory buffFactory = CreateTracked<FakeStackableBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Add", "Stack" }, _data.log);
    }

    [Test]
    public void RemoveHandler_AfterStacking_UnstacksWithoutStoppingHandler()
    {
        FakeStackableBuffFactory buffFactory = CreateTracked<FakeStackableBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        int stoppedCount = 0;
        _buffManager.OnBuffHandlerStopped.AddListener(_ => stoppedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add, Stack -> 2 stacks

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Unstack only, handler still has 1 stack

        CollectionAssert.AreEqual(new[] { "Add", "Stack", "Unstack" }, _data.log);
        Assert.AreEqual(0, stoppedCount);
    }

    [Test]
    public void RemoveBuffWithTag_StopsMatchingHandlerAndRemovesItsBuff()
    {
        // RemoveBuffWithTag properly tears the handler down: all stacks of its buff are removed at
        // once (a single Remove, no Unstack) and the handler is stopped, so the buff's effect never
        // stays attached to its target.
        GameplayTag tag = CreateTracked<GameplayTag>();
        FakeStackableBuffFactory buffFactory = CreateTracked<FakeStackableBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration, tags: new List<GameplayTag> { tag });
        int stoppedCount = 0;
        _buffManager.OnBuffHandlerStopped.AddListener(_ => stoppedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add, Stack -> 2 stacks

        _buffManager.RemoveBuffWithTag(tag);
        _buffManager.ForceUpdate(); // The handler is gone: nothing is re-applied

        CollectionAssert.AreEqual(new[] { "Add", "Stack", "Remove" }, _data.log);
        Assert.AreEqual(1, stoppedCount);
    }

    [Test]
    public void RemoveBuffWithoutTag_KeepsHandlerThatHasTheTag()
    {
        GameplayTag tag = CreateTracked<GameplayTag>();
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration, tags: new List<GameplayTag> { tag });
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add

        _buffManager.RemoveBuffWithoutTag(tag);
        _buffManager.ForceUpdate();

        // The handler has the tag, so RemoveBuffWithoutTag leaves it alone: no further log entries.
        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
    }
}

}
