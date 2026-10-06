using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Second Wind: the first time the holder falls below a health threshold in a battle, it gets a buff.
// The battle start comes from AscensionGameType in the game, the buff is rearmed directly here
public class ApplyBuffBelowHealthBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    Entity _owner;
    ApplyBuffBelowHealthBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _owner.attributeManager = _owner.GetComponent<AttributeManager>();
        _owner.attributeManager.Add(AttributeType.Damage, new Attribute(10f));
        BuffManager buffManager = _owner.GetComponent<BuffManager>();
        buffManager.isEnabled = true;
        TestHelpers.SetPrivateField(_owner, "_buffManager", buffManager);

        // Each time it's given, the owner deals 5 more damage: counts how often it's given
        FlatModifierFactory damage = ScriptableObject.CreateInstance<FlatModifierFactory>();
        damage.data = new FlatModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 5f };
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { damage } };
        _scriptableObjects.Add(damage);
        _scriptableObjects.Add(handler);

        _buff = new ApplyBuffBelowHealthBuff { data = new ApplyBuffBelowHealthBuffData { threshold = 0.25f, buffHandlerFactory = handler } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);
        _units.DestroyAll();
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    void ChangeHealth(float value)
    {
        ResourceModifier modifier = new ResourceModifier { source = _owner.gameObject };
        modifier.consumers.Add(new RuntimeConsumer(value));
        _owner.health.AddResourceModifier(modifier);
        TestUnits.Process(_owner.health);
    }

    float GetDamage()
    {
        _owner.GetComponent<BuffManager>().ForceUpdate();
        Attribute damage = _owner.attributeManager.Get(AttributeType.Damage);
        damage.Update();
        return damage.Value;
    }

    [Test]
    public void Health_AboveTheThreshold_GivesNothing()
    {
        ChangeHealth(-70f);

        Assert.IsFalse(_buff.isTriggered);
        Assert.AreEqual(10f, GetDamage(), 0.0001f);
    }

    [Test]
    public void Health_FallingBelowTheThreshold_GivesTheBuff()
    {
        ChangeHealth(-80f);

        Assert.IsTrue(_buff.isTriggered);
        Assert.AreEqual(15f, GetDamage(), 0.0001f);
    }

    [Test]
    public void Health_FallingBelowAgainInTheSameBattle_GivesTheBuffOnce()
    {
        ChangeHealth(-80f);
        ChangeHealth(60f);
        ChangeHealth(-60f);

        Assert.AreEqual(15f, GetDamage(), 0.0001f);
    }

    [Test]
    public void Health_FallingBelowInTheNextBattle_GivesTheBuffAgain()
    {
        ChangeHealth(-80f);
        ChangeHealth(60f);

        _buff.Rearm();
        ChangeHealth(-60f);

        Assert.AreEqual(20f, GetDamage(), 0.0001f);
    }

    [Test]
    public void Health_Lethal_GivesNothing()
    {
        ChangeHealth(-100f);

        Assert.IsFalse(_buff.isTriggered);
        Assert.AreEqual(10f, GetDamage(), 0.0001f);
    }

    [Test]
    public void Health_AfterRemove_GivesNothing()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        ChangeHealth(-80f);

        Assert.AreEqual(10f, GetDamage(), 0.0001f);
    }
}

}
