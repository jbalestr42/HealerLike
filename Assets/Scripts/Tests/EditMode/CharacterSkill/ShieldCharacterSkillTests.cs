using System.Collections.Generic;
using Buff;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Shield: spends all the Devotion, its target getting one stack of each buff per Devotion spent
public class ShieldCharacterSkillTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    GameObject _characterGo;
    ResourceAttribute _devotion;
    Entity _target;
    BuffManager _buffManager;
    BuffHandlerFactory _handlerFactory;
    ShieldCharacterSkill _skill;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        _devotion = TestHelpers.CreateResourceAttribute(_characterGo, ResourceType.Devotion, AttributeType.DevotionMax, 5f);
        _target = _units.Create(50f, 100f, "Target");
        _buffManager = _target.GetComponent<BuffManager>();
        _buffManager.isEnabled = true;
        // Adding Entity in TestUnits skips Init(): IBuffable needs its BuffManager
        TestHelpers.SetPrivateField(_target, "_buffManager", _buffManager);

        FakeStackableBuffFactory buff = ScriptableObject.CreateInstance<FakeStackableBuffFactory>();
        buff.data = new FakeBuffData();
        _handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 5f,
            maxStacks = 5,
            buffFactoryList = new List<ABuffFactory> { buff },
            tags = new List<GameplayTag>(),
        };
        _scriptableObjects.Add(buff);
        _scriptableObjects.Add(_handlerFactory);

        _skill = new ShieldCharacterSkill { data = new ShieldCharacterSkillData { buffHandlerFactory = new List<ABuffHandlerFactory> { _handlerFactory }, spentResource = ResourceType.Devotion } };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    int GetStacks()
    {
        _buffManager.ForceUpdate();
        BuffManager.BuffHandlerData handler = _buffManager.GetActiveHandlers().Find(h => h.buffHandlerFactory == _handlerFactory);
        return handler != null ? handler.currentStacks : 0;
    }

    [Test]
    public void OneStackPerDevotion_ThenTheDevotionIsEmptied()
    {
        _devotion.SetValue(3f);

        _skill.ApplyOnTarget(_characterGo, _target.gameObject);
        TestHelpers.InvokePrivate(_devotion, "Update");

        Assert.AreEqual(3, GetStacks());
        Assert.AreEqual(0f, _devotion.Value, 0.001f);
    }

    [Test]
    public void NoDevotion_GivesNothing()
    {
        _devotion.SetValue(0f);

        _skill.ApplyOnTarget(_characterGo, _target.gameObject);

        Assert.AreEqual(0, GetStacks());
    }

    [Test]
    public void ACharacterWithoutDevotion_GivesNothing()
    {
        GameObject other = new GameObject("Other");
        try
        {
            _skill.ApplyOnTarget(other, _target.gameObject);

            Assert.AreEqual(0, GetStacks());
        }
        finally
        {
            Object.DestroyImmediate(other);
        }
    }
}

}
