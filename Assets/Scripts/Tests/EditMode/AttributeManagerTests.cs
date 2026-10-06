using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

public class AttributeManagerTests
{
    GameObject _go;
    AttributeManager _attributeManager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject();
        _attributeManager = TestHelpers.CreateAttributeManager(_go);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void GetOrAdd_MissingAttribute_AddsItAtZero()
    {
        Attribute attribute = _attributeManager.GetOrAdd(AttributeType.Vulnerability);

        Assert.IsTrue(_attributeManager.Has(AttributeType.Vulnerability));
        Assert.AreEqual(0f, attribute.BaseValue);
        Assert.AreEqual(0f, attribute.Value);
    }

    [Test]
    public void GetOrAdd_MissingAttributeWithDefault_AddsItAtTheDefault()
    {
        Attribute attribute = _attributeManager.GetOrAdd(AttributeType.HealingReceived, 1f);

        Assert.AreEqual(1f, attribute.BaseValue);
        Assert.AreEqual(1f, attribute.Value);
    }

    [Test]
    public void GetOrAdd_MissingHealingReceived_StartsAt1SoHealsAreUntouched()
    {
        Assert.AreEqual(1f, _attributeManager.GetOrAdd(AttributeType.HealingReceived).Value);
    }

    [Test]
    public void GetOrAdd_MissingCriticalMultiplier_StartsAt1Point5()
    {
        Assert.AreEqual(1.5f, _attributeManager.GetOrAdd(AttributeType.CriticalMultiplier).Value);
    }

    [Test]
    public void GetOrAdd_MissingHealCriticalChance_StartsAt0()
    {
        Assert.AreEqual(0f, _attributeManager.GetOrAdd(AttributeType.HealCriticalChance).Value);
    }

    [Test]
    public void GetOrAdd_ExistingAttribute_KeepsItsValueAndIgnoresTheDefault()
    {
        Attribute existing = _attributeManager.Add(AttributeType.HealingReceived, new Attribute(0.5f));

        Attribute attribute = _attributeManager.GetOrAdd(AttributeType.HealingReceived, 1f);

        Assert.AreSame(existing, attribute);
        Assert.AreEqual(0.5f, attribute.Value);
    }
}

}
