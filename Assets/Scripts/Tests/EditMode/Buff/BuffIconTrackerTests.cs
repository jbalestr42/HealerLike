using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class BuffIconTrackerTests
{
    GameObject _source;
    GameObject _otherSource;
    GameObject _target;
    BuffManager _buffManager;
    BuffIconTracker _tracker;
    Sprite _sprite;
    readonly List<(ABuffHandlerFactory factory, int stacks)> _events = new List<(ABuffHandlerFactory, int)>();
    readonly List<Object> _objects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _otherSource = new GameObject("OtherSource");
        _target = new GameObject("Target");
        _buffManager = new GameObject("BuffManager").AddComponent<BuffManager>();
        _buffManager.isEnabled = true;
        _tracker = new BuffIconTracker(_buffManager);
        _tracker.OnStacksChanged.AddListener((factory, stacks) => _events.Add((factory, stacks)));

        Texture2D texture = new Texture2D(4, 4);
        _objects.Add(texture);
        _sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
        _objects.Add(_sprite);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_otherSource);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_buffManager.gameObject);
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
        _events.Clear();
    }

    ABuffHandlerFactory CreateHandlerFactory(Sprite icon, List<GameplayTag> tags = null)
    {
        FakeStackableBuffFactory buffFactory = ScriptableObject.CreateInstance<FakeStackableBuffFactory>();
        buffFactory.data = new FakeBuffData();
        _objects.Add(buffFactory);

        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { buffFactory },
            tags = tags ?? new List<GameplayTag>(),
            icon = icon,
        };
        _objects.Add(handlerFactory);
        return handlerFactory;
    }

    [Test]
    public void HandlerWithIcon_ReportsItsStacks()
    {
        ABuffHandlerFactory poison = CreateHandlerFactory(_sprite);

        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(2, _tracker.GetStacks(poison));
        Assert.AreEqual((poison, 2), _events[_events.Count - 1]);
    }

    [Test]
    public void HandlerWithoutIcon_IsNotTracked()
    {
        ABuffHandlerFactory passive = CreateHandlerFactory(null);

        _buffManager.AddHandler(passive, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(0, _tracker.GetStacks(passive));
        Assert.IsEmpty(_events);
    }

    [Test]
    public void RemovingOneStack_DecreasesTheStacks()
    {
        ABuffHandlerFactory poison = CreateHandlerFactory(_sprite);
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.ForceUpdate();

        _buffManager.RemoveHandler(poison, _source, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(1, _tracker.GetStacks(poison));
        Assert.AreEqual((poison, 1), _events[_events.Count - 1]);
    }

    [Test]
    public void StoppedHandler_ReportsZeroStacks()
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        _objects.Add(tag);
        ABuffHandlerFactory poison = CreateHandlerFactory(_sprite, new List<GameplayTag> { tag });
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.ForceUpdate();

        _buffManager.RemoveBuffWithTag(tag);

        Assert.AreEqual(0, _tracker.GetStacks(poison));
        Assert.AreEqual((poison, 0), _events[_events.Count - 1]);
    }

    [Test]
    public void SameEffectFromTwoSources_SumsTheStacks()
    {
        ABuffHandlerFactory poison = CreateHandlerFactory(_sprite);
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.AddHandler(poison, _otherSource, _target);
        _buffManager.AddHandler(poison, _otherSource, _target);
        _buffManager.ForceUpdate();

        Assert.AreEqual(3, _tracker.GetStacks(poison));
        Assert.AreEqual((poison, 3), _events[_events.Count - 1]);
    }

    [Test]
    public void StoppingOneSource_KeepsTheOtherSourceStacks()
    {
        ABuffHandlerFactory poison = CreateHandlerFactory(_sprite);
        _buffManager.AddHandler(poison, _source, _target);
        _buffManager.AddHandler(poison, _otherSource, _target);
        _buffManager.AddHandler(poison, _otherSource, _target);
        _buffManager.ForceUpdate();

        _buffManager.RemoveBuff(buffHandlerData => buffHandlerData.buffHandlerFactory == poison && buffHandlerData.currentStacks == 1);

        Assert.AreEqual(2, _tracker.GetStacks(poison));
        Assert.AreEqual((poison, 2), _events[_events.Count - 1]);
    }
}

}
