using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Radiant Archer, Paladin: each heal received by the holder gives it a buff
public class ApplyBuffOnHealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    Entity _owner;
    BuffManager _buffManager;
    ABuffHandlerFactory _handlerFactory;
    ApplyBuffOnHealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(50f, 100f, "Owner");
        _buffManager = _owner.GetComponent<BuffManager>();
        _buffManager.isEnabled = true;
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    // The buff given on heal: 10s, up to maxStacks stacks
    void AddBuff(int maxStacks)
    {
        FakeBuffFactory fakeBuff = ScriptableObject.CreateInstance<FakeBuffFactory>();
        fakeBuff.data = new FakeBuffData();
        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 10f,
            maxStacks = maxStacks,
            buffFactoryList = new List<ABuffFactory> { fakeBuff },
        };
        _scriptableObjects.Add(fakeBuff);
        _scriptableObjects.Add(handlerFactory);
        _handlerFactory = handlerFactory;

        _buff = new ApplyBuffOnHealBuff { data = new ApplyBuffOnHealBuffData { buffHandlerFactory = _handlerFactory } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    void Heal(float amount)
    {
        Entity.NotifyHealed(null, _owner.gameObject, new ConsumerResult(amount, false));
        _buffManager.ForceUpdate();
    }

    BuffManager.BuffHandlerData GetHandler()
    {
        return _buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _handlerFactory);
    }

    [Test]
    public void Heal_Received_GivesTheBuffToTheHolder_FromItself()
    {
        AddBuff(1);

        Heal(10f);

        BuffManager.BuffHandlerData handler = GetHandler();
        Assert.IsNotNull(handler);
        Assert.AreSame(_owner.gameObject, handler.source);
        Assert.AreSame(_owner.gameObject, handler.target);
    }

    [Test]
    public void NoHeal_NoBuff()
    {
        AddBuff(1);

        _buffManager.ForceUpdate();

        Assert.IsNull(GetHandler());
    }

    [Test]
    public void Damage_Received_GivesNoBuff()
    {
        AddBuff(1);

        Heal(-10f);

        Assert.IsNull(GetHandler());
    }

    // Paladin: +1 armor per heal, up to 3
    [Test]
    public void Heals_AddAStackEach_UpToTheMaxStacks()
    {
        AddBuff(3);

        Heal(10f);
        Assert.AreEqual(1, GetHandler().currentStacks);
        Heal(10f);
        Heal(10f);
        Heal(10f);

        Assert.AreEqual(3, GetHandler().currentStacks);
    }

    // Radiant Archer: a new heal while buffed gives it its whole duration again
    [Test]
    public void Heal_WhileBuffed_RefreshesTheDuration()
    {
        AddBuff(1);
        Heal(10f);
        ((BuffHandler)GetHandler().buffHandler).Update(8f);

        Heal(10f);

        Assert.AreEqual(10f, GetHandler().buffHandler.remainingDuration, 0.1f);
        Assert.AreEqual(1, GetHandler().currentStacks);
    }

    [Test]
    public void Heal_AfterRemove_GivesNoBuff()
    {
        AddBuff(1);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Heal(10f);

        Assert.IsNull(GetHandler());
    }

    [Test]
    public void Heal_WithoutBuffToGive_DoesNothing()
    {
        _buff = new ApplyBuffOnHealBuff { data = new ApplyBuffOnHealBuffData() };
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Assert.DoesNotThrow(() => Heal(10f));
        Assert.IsEmpty(_buffManager.GetActiveHandlers());
    }
}

}
