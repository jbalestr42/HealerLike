using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The allies come from the EntityManager singleton in the game
public class TestSoulLinkBuff : SoulLinkBuff
{
    public List<GameObject> allies = new List<GameObject>();

    protected override List<GameObject> GetAllies() => allies;
}

// Soul Link: the damage dealt by the enemies to the holder is split evenly between it and every other living ally
public class SoulLinkBuffTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _attacker;
    Entity _holder;
    Entity _ally;
    Entity _otherAlly;
    List<GameObject> _allies;

    [SetUp]
    public void SetUp()
    {
        _attacker = new GameObject("Attacker");
        TestHelpers.CreateAttributeManager(_attacker);
        _holder = _units.Create(100f, 100f, "Holder");
        _ally = _units.Create(100f, 100f, "Ally");
        _otherAlly = _units.Create(100f, 100f, "Other Ally");
        _allies = new List<GameObject> { _holder.gameObject, _ally.gameObject, _otherAlly.gameObject };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_attacker);
    }

    TestSoulLinkBuff Link(Entity entity)
    {
        TestSoulLinkBuff buff = new TestSoulLinkBuff { data = new SoulLinkBuffData(), allies = _allies };
        buff.Add(entity.gameObject, entity.gameObject);
        return buff;
    }

    // A regular hit, going through armor and invincibility
    void Hit(Entity entity, float damage)
    {
        ResourceModifier modifier = new ResourceModifier { source = _attacker };
        modifier.consumers.Add(new RuntimeConsumer(-damage, ignoreDamageReduction: false, ignoreConsumerPrevention: false));
        entity.health.AddResourceModifier(modifier);
        TestUnits.Process(entity.health);
    }

    void ProcessAll()
    {
        foreach (GameObject go in _allies)
        {
            TestUnits.Process(go.GetComponent<Entity>().health);
        }
    }

    static void SetAttribute(Entity entity, AttributeType type, float value)
    {
        Attribute attribute = entity.health.GetComponent<AttributeManager>().GetOrAdd(type);
        attribute.BaseValue = value;
        attribute.Update();
    }

    [Test]
    public void Damage_IsSplitEvenlyBetweenTheHolderAndEveryAlly()
    {
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(90f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(90f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(90f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void ArmorOfTheHolder_ReducesTheDamageBeforeTheSplit_AndTheArmorOfTheAlliesDoesNotApplyAgain()
    {
        SetAttribute(_holder, AttributeType.PercentArmor, 0.5f);
        SetAttribute(_ally, AttributeType.PercentArmor, 0.5f);
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        // 15 damage after the armor of the holder, 5 each
        Assert.AreEqual(95f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(95f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(95f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void HitBlockedByTheHitArmorOfTheHolder_SharesNothing()
    {
        SetAttribute(_holder, AttributeType.HitArmor, 1f);
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(100f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(100f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(100f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void DeadAllies_TakeNoShare()
    {
        _otherAlly.health.SetValue(0f);
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(85f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(85f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(0f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void HolderAlone_TakesTheWholeDamage()
    {
        _allies = new List<GameObject> { _holder.gameObject };
        Link(_holder);

        Hit(_holder, 30f);

        Assert.AreEqual(70f, _holder.health.Value, 0.0001f);
    }

    [Test]
    public void InvincibleAlly_TakesNothing_AndTheHolderStillTakesOnlyItsShare()
    {
        _ally.health.preventConsumers = true;
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(90f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(100f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(90f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void Heals_AreNotShared()
    {
        _holder.health.SetValue(50f);
        Link(_holder);
        ResourceModifier heal = new ResourceModifier { source = _attacker };
        heal.consumers.Add(new RuntimeConsumer(30f));
        _holder.health.AddResourceModifier(heal);

        ProcessAll();

        Assert.AreEqual(80f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(100f, _ally.health.Value, 0.0001f);
    }

    [Test]
    public void OnceRemoved_TheHolderTakesTheWholeDamageAgain()
    {
        TestSoulLinkBuff buff = Link(_holder);
        buff.Remove(_holder.gameObject, _holder.gameObject);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(70f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(100f, _ally.health.Value, 0.0001f);
    }

    [Test]
    public void ShareReceivedByAnotherLinkedAlly_IsNotSplitAgain()
    {
        Link(_holder);
        Link(_ally);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(90f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(90f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(90f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void DamageFromAnAlly_IsNotSplit()
    {
        Link(_holder);
        ResourceModifier selfDamage = new ResourceModifier { source = _ally.gameObject };
        selfDamage.consumers.Add(new RuntimeConsumer(-30f, ignoreDamageReduction: false, ignoreConsumerPrevention: false));
        _holder.health.AddResourceModifier(selfDamage);

        ProcessAll();

        Assert.AreEqual(70f, _holder.health.Value, 0.0001f);
        Assert.AreEqual(100f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(100f, _otherAlly.health.Value, 0.0001f);
    }

    [Test]
    public void Shares_CanNotBeCritical()
    {
        // The shares are sent by the holder: its critical chance must not apply to them
        AttributeManager holderAttributes = _holder.GetComponent<AttributeManager>();
        holderAttributes.Add(AttributeType.CriticalChance, new Attribute(1000f));
        holderAttributes.Add(AttributeType.CriticalMultiplier, new Attribute(2f));
        Link(_holder);

        Hit(_holder, 30f);
        ProcessAll();

        Assert.AreEqual(90f, _ally.health.Value, 0.0001f);
        Assert.AreEqual(90f, _otherAlly.health.Value, 0.0001f);
    }
}

}
