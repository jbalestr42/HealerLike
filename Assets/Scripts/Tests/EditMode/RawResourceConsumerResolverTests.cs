using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

// The mana and the Devotion of the character: the consumers taken as they are, no armor, vulnerability nor critical
public class RawResourceConsumerResolverTests
{
    GameObject _ownerGo;
    GameObject _sourceGo;
    AttributeManager _ownerAttributes;
    ResourceAttribute _mana;

    [SetUp]
    public void SetUp()
    {
        _ownerGo = new GameObject("Character");
        _mana = TestHelpers.CreateResourceAttribute(_ownerGo, ResourceType.Mana, AttributeType.ManaMax, 100f, new RawResourceConsumerResolver());
        _mana.SetValue(50f);
        _ownerAttributes = _ownerGo.GetComponent<AttributeManager>();

        _sourceGo = new GameObject("Source");
        AttributeManager sourceAttributes = TestHelpers.CreateAttributeManager(_sourceGo);
        sourceAttributes.Add(AttributeType.CriticalChance, new Attribute(100f));
        sourceAttributes.Add(AttributeType.HealCriticalChance, new Attribute(100f));
        sourceAttributes.Add(AttributeType.CriticalMultiplier, new Attribute(2f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ownerGo);
        Object.DestroyImmediate(_sourceGo);
    }

    // A change that could be reduced and critical, like the damage of a unit
    void Change(float value)
    {
        ResourceModifier modifier = new ResourceModifier { source = _sourceGo };
        modifier.consumers.Add(new RuntimeConsumer(value, ignoreDamageReduction: false, ignoreConsumerPrevention: false, canBeCritical: true));
        _mana.AddResourceModifier(modifier);
        TestHelpers.InvokePrivate(_mana, "Update");
    }

    [Test]
    public void Gain_NeverCritical_EvenAGuaranteedOne()
    {
        Change(10f);

        Assert.AreEqual(60f, _mana.Value, 0.0001f);
    }

    [Test]
    public void Cost_NeverCritical_EvenAGuaranteedOne()
    {
        Change(-10f);

        Assert.AreEqual(40f, _mana.Value, 0.0001f);
    }

    [Test]
    public void Cost_NotReducedByArmor_NorIncreasedByVulnerability()
    {
        _ownerAttributes.Add(AttributeType.FlatArmor, new Attribute(3f));
        _ownerAttributes.Add(AttributeType.PercentArmor, new Attribute(0.5f));
        _ownerAttributes.Add(AttributeType.Vulnerability, new Attribute(0.5f));

        Change(-10f);

        Assert.AreEqual(40f, _mana.Value, 0.0001f);
    }

    [Test]
    public void Cost_NotBlockedByHitArmor_WhichIsKept()
    {
        Attribute hitArmor = new Attribute(1f);
        _ownerAttributes.Add(AttributeType.HitArmor, hitArmor);

        Change(-10f);

        Assert.AreEqual(40f, _mana.Value, 0.0001f);
        Assert.AreEqual(1f, hitArmor.Value, "no hit used up");
    }

    // The armor attributes are only added to the owner by the resolvers using them
    [Test]
    public void Init_AddsNoArmorNorResistToTheOwner()
    {
        Assert.IsFalse(_ownerAttributes.Has(AttributeType.PercentArmor));
        Assert.IsFalse(_ownerAttributes.Has(AttributeType.FlatArmor));
        Assert.IsFalse(_ownerAttributes.Has(AttributeType.HitArmor));
        Assert.IsFalse(_ownerAttributes.Has(AttributeType.Vulnerability));
        Assert.IsFalse(_ownerAttributes.Has(AttributeType.CriticalChanceResist));
    }

    // Still a resource: the consumers are stopped while it prevents them, unless they ignore it
    [Test]
    public void PreventedConsumers_StillPrevented()
    {
        _mana.preventConsumers = true;

        Change(-10f);

        Assert.AreEqual(50f, _mana.Value, 0.0001f);
    }

    [Test]
    public void GainMultiplier_StillApplies()
    {
        GameObject go = new GameObject("Unit");
        try
        {
            ResourceAttribute health = TestHelpers.CreateResourceAttribute(go, ResourceType.Health, AttributeType.HealthMax, 100f, new RawResourceConsumerResolver(AttributeType.HealingReceived));
            health.SetValue(50f);
            Attribute healingReceived = go.GetComponent<AttributeManager>().Get(AttributeType.HealingReceived);
            healingReceived.BaseValue = 0.5f;
            healingReceived.Update();
            ResourceModifier modifier = new ResourceModifier { source = _sourceGo };
            modifier.consumers.Add(new RuntimeConsumer(10f));
            health.AddResourceModifier(modifier);
            TestHelpers.InvokePrivate(health, "Update");

            Assert.AreEqual(55f, health.Value, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}
