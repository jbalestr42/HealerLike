using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Gives some of a resource to its target (e.g. the Shield Wardens recharging the shield of the Iron Matron)
public class GainResourceBuffTests
{
    readonly List<GameObject> _objects = new List<GameObject>();
    GameObject _source;
    GameObject _target;
    ResourceAttribute _shield;

    [SetUp]
    public void SetUp()
    {
        _source = Create("Source");
        TestHelpers.CreateAttributeManager(_source, AttributeType.HealthMax, 50f);
        // A target with an empty shield of 200 max
        _target = Create("Target");
        _shield = TestHelpers.CreateResourceAttribute(_target, ResourceType.Shield, AttributeType.HealthMax, 200f);
        _shield.fillWithMax = false;
        _shield.SetValue(0f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    GameObject Create(string name)
    {
        GameObject go = new GameObject(name);
        _objects.Add(go);
        return go;
    }

    // 10% of the max health of the value owner
    static GainResourceBuff CreateBuff(ResourceType resourceType, ConsumerValueOwner valueOwner, bool upToValue = false)
    {
        AValue value = new AttributeValue { data = new AttributeValueData { type = AttributeType.HealthMax, multiplier = 0.1f } };
        return new GainResourceBuff { data = new GainResourceBuffData { resourceType = resourceType, value = value, valueOwner = valueOwner, upToValue = upToValue } };
    }

    float ShieldAfter(GainResourceBuff buff)
    {
        buff.Instant(_source, _target);
        TestUnits.Process(_shield);
        return _shield.Value;
    }

    [Test]
    public void Instant_GivesTheResource_ReadOnTheTarget()
    {
        Assert.AreEqual(20f, ShieldAfter(CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target)), 0.0001f);
    }

    [Test]
    public void Instant_GivesTheResource_ReadOnTheSource()
    {
        Assert.AreEqual(5f, ShieldAfter(CreateBuff(ResourceType.Shield, ConsumerValueOwner.Source)), 0.0001f);
    }

    [Test]
    public void Instant_SeveralTimes_AddsUp()
    {
        GainResourceBuff buff = CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target);
        ShieldAfter(buff);

        Assert.AreEqual(40f, ShieldAfter(buff), 0.0001f);
    }

    [Test]
    public void Instant_NeverAboveTheMax()
    {
        _shield.SetValue(190f);

        Assert.AreEqual(200f, ShieldAfter(CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target)), 0.0001f);
    }

    [Test]
    public void Instant_UpToValue_TopsUpToTheAmount()
    {
        _shield.SetValue(5f);

        Assert.AreEqual(20f, ShieldAfter(CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target, upToValue: true)), 0.0001f);
    }

    [Test]
    public void Instant_UpToValue_SeveralTimes_NeverPilesUp()
    {
        GainResourceBuff buff = CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target, upToValue: true);
        ShieldAfter(buff);

        Assert.AreEqual(20f, ShieldAfter(buff), 0.0001f);
    }

    [Test]
    public void Instant_UpToValue_AlreadyAbove_LeavesItUntouched()
    {
        _shield.SetValue(150f);

        Assert.AreEqual(150f, ShieldAfter(CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target, upToValue: true)), 0.0001f);
    }

    [Test]
    public void Instant_OtherResourceType_LeavesTheShieldUntouched()
    {
        Assert.AreEqual(0f, ShieldAfter(CreateBuff(ResourceType.Mana, ConsumerValueOwner.Target)), 0.0001f);
    }

    [Test]
    public void Instant_TargetWithoutTheResource_DoesNothing()
    {
        GameObject bare = Create("Bare");
        TestHelpers.CreateAttributeManager(bare, AttributeType.HealthMax, 100f);

        Assert.DoesNotThrow(() => CreateBuff(ResourceType.Shield, ConsumerValueOwner.Target).Instant(_source, bare));
    }
}

}
