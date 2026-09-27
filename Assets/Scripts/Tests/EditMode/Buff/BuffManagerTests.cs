using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

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

// Listens to a static event while applied, like the round end buffs of the player items
public class StaticEventBuff : ABuff<FakeBuffData>
{
    public static UnityEvent OnEvent = new UnityEvent();

    void OnEventInvoked() => data.log.Add("Event");

    public override void Instant(GameObject source, GameObject target) { }
    public override void Add(GameObject source, GameObject target) => OnEvent.AddListener(OnEventInvoked);
    public override void Remove(GameObject source, GameObject target) => OnEvent.RemoveListener(OnEventInvoked);
}

public class FakeBuffFactory : BuffFactory<FakeBuff, FakeBuffData> { }
public class StaticEventBuffFactory : BuffFactory<StaticEventBuff, FakeBuffData> { }
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
        StaticEventBuff.OnEvent.RemoveAllListeners();
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

    ABuffHandlerFactory CreateStackableHandlerFactory(int maxStacks, DurationType durationType = DurationType.Duration)
    {
        FakeStackableBuffFactory buffFactory = CreateTracked<FakeStackableBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, durationType);
        ((BuffHandlerFactory)handlerFactory).data.maxStacks = maxStacks;
        return handlerFactory;
    }

    [Test]
    public void MaxStacks_ByDefault_IsUnlimited()
    {
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(0);

        for (int i = 0; i < 5; i++)
        {
            _buffManager.AddHandler(handlerFactory, _source, _target);
            _buffManager.ForceUpdate();
        }

        CollectionAssert.AreEqual(new[] { "Add", "Stack", "Stack", "Stack", "Stack" }, _data.log);
    }

    [Test]
    public void MaxStacks_ReachedAcrossUpdates_DoesNotStackAnymore()
    {
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(2);

        for (int i = 0; i < 4; i++)
        {
            _buffManager.AddHandler(handlerFactory, _source, _target);
            _buffManager.ForceUpdate();
        }

        CollectionAssert.AreEqual(new[] { "Add", "Stack" }, _data.log);
    }

    [Test]
    public void MaxStacks_ReachedWithinOneUpdate_DoesNotStackAnymore()
    {
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(1);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
    }

    [Test]
    public void MaxStacks_Reached_StillRefreshesTheDuration()
    {
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(1);
        int refreshedCount = 0;
        _buffManager.OnBuffHandlerRefreshed.AddListener(_ => refreshedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();
        BuffHandler handler = null;
        _buffManager.OnBuffHandlerRefreshed.AddListener(buffHandlerData => handler = (BuffHandler)buffHandlerData.buffHandler);
        _buffManager.GetActiveHandlers()[0].buffHandler.Update(3f);

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(2, refreshedCount);
        // The refresh reset the timer, only the frame's own delta time went by since
        Assert.Less(handler.durationTimer, 3f);
    }

    [Test]
    public void MaxStacks_IsPerSource()
    {
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(1);
        GameObject otherSource = new GameObject("OtherSource");

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, otherSource, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Add", "Add" }, _data.log);
        Object.DestroyImmediate(otherSource);
    }

    [Test]
    public void MaxStacks_RemovingARequestOverTheCap_KeepsTheAppliedStacks()
    {
        // Aura-like handlers are added then removed: a removal must first cancel the requests over the cap
        ABuffHandlerFactory handlerFactory = CreateStackableHandlerFactory(1, DurationType.Infinite);
        int stoppedCount = 0;
        _buffManager.OnBuffHandlerStopped.AddListener(_ => stoppedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // 2 requested, 1 applied

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // 1 requested, still 1 applied

        CollectionAssert.AreEqual(new[] { "Add" }, _data.log);
        Assert.AreEqual(0, stoppedCount);

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // 0 requested

        Assert.AreEqual("Remove", _data.log[^1]);
        Assert.AreEqual(1, stoppedCount);
    }

    [Test]
    public void StopHandler_NonStackableBuffAppliedTwice_RemovesEveryInstance()
    {
        // Each application of a non stackable buff adds its own instance (an InvincibilityBuff
        // counts them): stopping the handler must undo all of them, not only the first one
        GameplayTag tag = CreateTracked<GameplayTag>();
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration, tags: new List<GameplayTag> { tag });
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add, Add

        _buffManager.RemoveBuffWithTag(tag);

        CollectionAssert.AreEqual(new[] { "Add", "Add", "Remove", "Remove" }, _data.log);
    }

    [Test]
    public void RemoveHandler_NonStackableBuffAppliedTwice_RemovesOneInstanceAtATime()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Infinite);
        int stoppedCount = 0;
        _buffManager.OnBuffHandlerStopped.AddListener(_ => stoppedCount++);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add, Add

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // One instance left

        CollectionAssert.AreEqual(new[] { "Add", "Add", "Remove" }, _data.log);
        Assert.AreEqual(0, stoppedCount);

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.AreEqual(new[] { "Add", "Add", "Remove", "Remove" }, _data.log);
        Assert.AreEqual(1, stoppedCount);
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

    [Test]
    public void HasHandler_WithoutAnyHandler_IsFalse()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;

        Assert.IsFalse(_buffManager.HasHandler(CreateHandlerFactory(buffFactory, DurationType.Duration)));
    }

    [Test]
    public void HasHandler_RightAfterAddHandler_IsTrue()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);

        _buffManager.AddHandler(handlerFactory, _source, _target);

        // Before any update, so two skills checking in the same frame don't both pick this target
        Assert.IsTrue(_buffManager.HasHandler(handlerFactory));
    }

    [Test]
    public void HasHandler_FromAnotherSource_IsTrue()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        GameObject otherSource = new GameObject("OtherSource");

        _buffManager.AddHandler(handlerFactory, otherSource, _target);
        _buffManager.ForceUpdate();

        Assert.IsTrue(_buffManager.HasHandler(handlerFactory));
        Object.DestroyImmediate(otherSource);
    }

    [Test]
    public void HasHandler_WithAnotherHandler_IsFalse()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        ABuffHandlerFactory otherHandlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);

        _buffManager.AddHandler(otherHandlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        Assert.IsFalse(_buffManager.HasHandler(handlerFactory));
    }

    [Test]
    public void HasHandler_AfterTheHandlerStopped_IsFalse()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Add

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate(); // Remove + Stop

        Assert.IsFalse(_buffManager.HasHandler(handlerFactory));
    }

    [Test]
    public void HasHandler_AfterAnInstantHandlerApplied_IsFalse()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Instant);
        _buffManager.AddHandler(handlerFactory, _source, _target);

        _buffManager.ForceUpdate();

        Assert.IsFalse(_buffManager.HasHandler(handlerFactory));
    }

    [Test]
    public void GetActiveHandlers_WithoutHandler_IsEmpty()
    {
        CollectionAssert.IsEmpty(_buffManager.GetActiveHandlers());
    }

    [Test]
    public void GetActiveHandlers_BeforeTheHandlerStarted_IsEmpty()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;

        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Duration), _source, _target);

        CollectionAssert.IsEmpty(_buffManager.GetActiveHandlers());
    }

    [Test]
    public void GetActiveHandlers_ListsTheStartedHandlersOfEverySource()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        ABuffHandlerFactory otherHandlerFactory = CreateHandlerFactory(buffFactory, DurationType.Infinite);
        GameObject otherSource = new GameObject("OtherSource");

        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(otherHandlerFactory, otherSource, _target);
        _buffManager.ForceUpdate();

        List<BuffManager.BuffHandlerData> activeHandlers = _buffManager.GetActiveHandlers();
        Assert.AreEqual(2, activeHandlers.Count);
        CollectionAssert.AreEquivalent(new[] { handlerFactory, otherHandlerFactory }, activeHandlers.ConvertAll(buffHandlerData => buffHandlerData.buffHandlerFactory));
        Object.DestroyImmediate(otherSource);
    }

    [Test]
    public void GetActiveHandlers_InstantHandler_IsNeverListed()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;

        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Instant), _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.IsEmpty(_buffManager.GetActiveHandlers());
    }

    [Test]
    public void GetActiveHandlers_AfterTheHandlerStopped_IsEmpty()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Duration);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        _buffManager.RemoveHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        CollectionAssert.IsEmpty(_buffManager.GetActiveHandlers());
    }

    // Edit mode never sends OnDestroy() (nor Awake()) to a destroyed component: send it like Unity would
    void Destroy()
    {
        TestHelpers.InvokePrivate(_buffManager, "OnDestroy");
    }

    [Test]
    public void Destroy_RemovesEveryAppliedBuff()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Infinite), _source, _target);
        _buffManager.ForceUpdate();

        Destroy();

        CollectionAssert.AreEqual(new[] { "Add", "Remove" }, _data.log);
    }

    [Test]
    public void Destroy_StackedBuff_RemovesItOnce()
    {
        FakeStackableBuffFactory buffFactory = CreateTracked<FakeStackableBuffFactory>();
        buffFactory.data = _data;
        ABuffHandlerFactory handlerFactory = CreateHandlerFactory(buffFactory, DurationType.Infinite);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();

        Destroy();

        CollectionAssert.AreEqual(new[] { "Add", "Stack", "Remove" }, _data.log);
    }

    [Test]
    public void Destroy_HandlerNotStartedYet_RemovesNothing()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Infinite), _source, _target);

        Destroy();

        CollectionAssert.IsEmpty(_data.log);
    }

    [Test]
    public void Destroy_DoesNotNotifyTheListeners()
    {
        FakeBuffFactory buffFactory = CreateTracked<FakeBuffFactory>();
        buffFactory.data = _data;
        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Infinite), _source, _target);
        _buffManager.ForceUpdate();
        bool isNotified = false;
        _buffManager.OnBuffRemoved.AddListener(buffData => isNotified = true);
        _buffManager.OnBuffHandlerStopped.AddListener(buffHandlerData => isNotified = true);

        Destroy();

        Assert.IsFalse(isNotified);
    }

    [Test]
    public void Destroy_BuffListeningToAStaticEvent_StopsReactingToIt()
    {
        StaticEventBuffFactory buffFactory = CreateTracked<StaticEventBuffFactory>();
        buffFactory.data = _data;
        _buffManager.AddHandler(CreateHandlerFactory(buffFactory, DurationType.Infinite), _source, _target);
        _buffManager.ForceUpdate();
        StaticEventBuff.OnEvent.Invoke();

        Destroy();
        StaticEventBuff.OnEvent.Invoke();

        CollectionAssert.AreEqual(new[] { "Event" }, _data.log);
    }
}

}
