using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Buff
{

// A status (e.g. Blessed): given through its buff, checked through its tag, the buff giving the tag
public class StatusDataTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    GameplayTag _tag;
    AddTagBuffFactory _addTag;
    BuffHandlerFactory _handler;
    StatusData _status;
    Entity _unit;
    GameObject _source;

    [SetUp]
    public void SetUp()
    {
        _tag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        _tag.name = "Blessed";
        _addTag = Track(ScriptableObject.CreateInstance<AddTagBuffFactory>());
        _addTag.data = new AddTagBuffData { tag = _tag };
        _handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 4f,
            maxStacks = 1,
            buffFactoryList = new List<ABuffFactory> { _addTag },
            tags = new List<GameplayTag> { _tag },
        };
        _status = Track(ScriptableObject.CreateInstance<StatusData>());
        _status.tag = _tag;
        _status.buffHandlerFactory = _handler;

        _unit = _units.Create(100f, 100f, "Unit");
        _unit.GetComponent<BuffManager>().isEnabled = true;
        _source = new GameObject("Source");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_source);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T Track<T>(T scriptableObject) where T : Object
    {
        _scriptableObjects.Add(scriptableObject);
        return scriptableObject;
    }

    void Update()
    {
        _unit.GetComponent<BuffManager>().ForceUpdate();
    }

    [Test]
    public void ApplyStatus_GivesItsBuff_FromTheSource()
    {
        _unit.ApplyStatus(_status, _source);
        Update();

        BuffManager.BuffHandlerData given = _unit.GetComponent<BuffManager>().GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _handler);
        Assert.IsNotNull(given);
        Assert.AreSame(_source, given.source);
    }

    [Test]
    public void ApplyStatus_TheUnitHasTheStatus()
    {
        Assert.IsFalse(_unit.HasStatus(_status));

        _unit.ApplyStatus(_status, _source);
        Update();

        Assert.IsTrue(_unit.HasStatus(_status));
        Assert.IsTrue(_unit.HasTag(_tag));
    }

    [Test]
    public void StatusRemoved_TheUnitNoLongerHasIt()
    {
        _unit.ApplyStatus(_status, _source);
        Update();

        _unit.GetComponent<BuffManager>().RemoveHandler(_handler, _source, _unit.gameObject);
        Update();

        Assert.IsFalse(_unit.HasStatus(_status));
    }

    // e.g. a unit tagged with a status descending from Blessed is blessed
    [Test]
    public void HasStatus_ADescendantTagCounts()
    {
        GameplayTag child = Track(ScriptableObject.CreateInstance<GameplayTag>());
        TestHelpers.SetPrivateField(child, "_parent", _tag);

        _unit.AddTag(child);

        Assert.IsTrue(_unit.HasStatus(_status));
    }

    [Test]
    public void ApplyStatus_WithoutStatus_GivesNothing()
    {
        _unit.ApplyStatus(null, _source);
        Update();

        CollectionAssert.IsEmpty(_unit.GetComponent<BuffManager>().GetActiveHandlers());
        Assert.IsFalse(_unit.HasStatus(null));
    }

    [Test]
    public void GivesItsTag_TheBuffGivesTheTagAndIsTaggedWithIt()
    {
        Assert.IsTrue(_status.GivesItsTag());
    }

    // The status would be given without the reactions to it ever seeing it
    [Test]
    public void GivesItsTag_TheBuffGivesAnotherTag_False()
    {
        GameplayTag other = Track(ScriptableObject.CreateInstance<GameplayTag>());
        _addTag.data.tag = other;

        Assert.IsFalse(_status.GivesItsTag());
    }

    // EachApplication reactions look for the tag on the buff given
    [Test]
    public void GivesItsTag_TheBuffNotTaggedWithIt_False()
    {
        _handler.data.tags = new List<GameplayTag>();

        Assert.IsFalse(_status.GivesItsTag());
    }

    // Every status of the game: a name, a description and a buff giving its tag
    [Test]
    public void EveryStatusAsset_IsComplete()
    {
        string[] guids = AssetDatabase.FindAssets("t:StatusData");
        Assert.IsNotEmpty(guids);
        foreach (string guid in guids)
        {
            StatusData status = AssetDatabase.LoadAssetAtPath<StatusData>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsFalse(string.IsNullOrEmpty(status.title), status.name);
            Assert.IsFalse(string.IsNullOrEmpty(status.description), status.name);
            Assert.IsTrue(status.GivesItsTag(), status.name);
        }
    }

    [Test]
    public void GivesItsTag_WithoutTagOrBuff_False()
    {
        _status.tag = null;
        Assert.IsFalse(_status.GivesItsTag());

        _status.tag = _tag;
        _status.buffHandlerFactory = null;
        Assert.IsFalse(_status.GivesItsTag());
    }
}

}
