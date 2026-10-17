using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Stacks of a status given like any buff handler (e.g. the Hexer making its target Vulnerable twice): they land on the
// buff of the status itself, every giver adding to the same stacks, up to the max of the status
public class StatusStacksFactoryTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    BuffHandlerFactory _handler;
    StatusData _status;
    Sprite _icon;
    Entity _unit;
    BuffManager _buffManager;
    GameObject _source;
    GameObject _otherSource;

    [SetUp]
    public void SetUp()
    {
        GameplayTag tag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        tag.name = "Vulnerable";
        AddTagBuffFactory addTag = Track(ScriptableObject.CreateInstance<AddTagBuffFactory>());
        addTag.data = new AddTagBuffData { tag = tag };
        FlatModifierFactory vulnerability = Track(ScriptableObject.CreateInstance<FlatModifierFactory>());
        vulnerability.data = new FlatModifierData { type = AttributeType.Vulnerability, modifierType = AttributeModifierType.Add, value = 0.15f };
        _icon = Track(Sprite.Create(Track(new Texture2D(2, 2)), new Rect(0f, 0f, 2f, 2f), Vector2.zero));
        _handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            maxStacks = 5,
            stackAcrossSources = true,
            icon = _icon,
            buffFactoryList = new List<ABuffFactory> { addTag, vulnerability },
            tags = new List<GameplayTag> { tag },
        };
        _status = Track(ScriptableObject.CreateInstance<StatusData>());
        _status.title = "Vulnerable";
        _status.tag = tag;
        _status.buffHandlerFactory = _handler;

        _unit = _units.Create(100f, 100f, "Unit");
        _buffManager = _unit.GetComponent<BuffManager>();
        _buffManager.isEnabled = true;
        _source = new GameObject("Source");
        _otherSource = new GameObject("Other Source");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_otherSource);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _scriptableObjects.Add(obj);
        return obj;
    }

    StatusStacksFactory Stacks(int stacks, StatusData status = null)
    {
        StatusStacksFactory statusStacks = Track(ScriptableObject.CreateInstance<StatusStacksFactory>());
        statusStacks.data = new StatusStacksData { status = status ?? _status, stacks = stacks };
        return statusStacks;
    }

    float Vulnerability()
    {
        _buffManager.ForceUpdate();
        Attribute vulnerability = _unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Vulnerability);
        vulnerability.Update();
        return vulnerability.Value;
    }

    [Test]
    public void AddHandler_GivesItsStacksOfTheBuffOfTheStatus()
    {
        _buffManager.AddHandler(Stacks(2), _source, _unit.gameObject);

        Assert.AreEqual(0.3f, Vulnerability(), 0.0001f);
        Assert.IsTrue(_unit.HasStatus(_status));
        BuffManager.BuffHandlerData given = _buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _handler);
        Assert.IsNotNull(given, "the buff of the status itself");
        Assert.AreEqual(2, given.currentStacks);
    }

    // E.g. the Ravager (1) and the Hexer (2) on the same unit
    [Test]
    public void TwoGivers_AddToTheSameStacks()
    {
        _buffManager.AddHandler(Stacks(1), _source, _unit.gameObject);
        _buffManager.AddHandler(Stacks(2), _otherSource, _unit.gameObject);

        Assert.AreEqual(0.45f, Vulnerability(), 0.0001f);
        Assert.AreEqual(1, _buffManager.GetActiveHandlers().FindAll(handler => handler.buffHandlerFactory == _handler).Count);
    }

    [Test]
    public void TheStatusGivenDirectly_AndItsStacks_AddUp()
    {
        _unit.ApplyStatus(_status, _source);
        _buffManager.AddHandler(Stacks(2), _otherSource, _unit.gameObject);

        Assert.AreEqual(0.45f, Vulnerability(), 0.0001f);
    }

    [Test]
    public void Stacks_NeverAboveTheMaxOfTheStatus()
    {
        _buffManager.AddHandler(Stacks(4), _source, _unit.gameObject);
        _buffManager.AddHandler(Stacks(4), _otherSource, _unit.gameObject);

        Assert.AreEqual(0.75f, Vulnerability(), 0.0001f, "5 stacks of 15%");
    }

    [Test]
    public void RemoveHandler_TakesItsStacksBack()
    {
        StatusStacksFactory three = Stacks(3);
        _buffManager.AddHandler(three, _source, _unit.gameObject);
        Assert.AreEqual(0.45f, Vulnerability(), 0.0001f);

        _buffManager.RemoveHandler(three, _source, _unit.gameObject);

        Assert.AreEqual(0f, Vulnerability(), 0.0001f);
        Assert.IsFalse(_unit.HasStatus(_status));
    }

    // E.g. the Hexer cursing only the units not Vulnerable yet, whoever made them so
    [Test]
    public void HasHandler_TheStatusFromAnyGiver()
    {
        StatusStacksFactory two = Stacks(2);
        Assert.IsFalse(_buffManager.HasHandler(two));

        _buffManager.AddHandler(Stacks(1), _otherSource, _unit.gameObject);
        _buffManager.ForceUpdate();

        Assert.IsTrue(_buffManager.HasHandler(two));
    }

    [Test]
    public void ReadsLikeTheBuffOfTheStatus()
    {
        StatusStacksFactory two = Stacks(2);

        Assert.AreSame(_handler, two.appliedFactory);
        Assert.AreEqual(2, two.appliedStacks);
        Assert.AreSame(_icon, two.icon);
        Assert.AreEqual(DurationType.Duration, two.durationType);
        Assert.AreEqual(6f, two.duration, 0.0001f);
        Assert.IsTrue(two.hasDuration);
        Assert.AreEqual(5, two.maxStacks);
        Assert.IsTrue(two.stackAcrossSources);
        CollectionAssert.AreEqual(_handler.buffFactoryList, two.buffFactoryList);
        CollectionAssert.AreEqual(_handler.tags, two.tags);
        Assert.IsTrue(two.HasTag(_status.tag));
    }

    [Test]
    public void AtLeastOneStack()
    {
        Assert.AreEqual(1, Stacks(0).appliedStacks);
    }

    [Test]
    public void WithoutStatus_GivesNothing()
    {
        StatusStacksFactory none = Track(ScriptableObject.CreateInstance<StatusStacksFactory>());

        Assert.DoesNotThrow(() => _buffManager.AddHandler(none, _source, _unit.gameObject));
        Assert.AreEqual(0f, Vulnerability(), 0.0001f);
        Assert.IsFalse(_buffManager.HasHandler(none));
        Assert.IsNull(none.icon);
        CollectionAssert.IsEmpty(none.buffFactoryList);
    }
}

}
