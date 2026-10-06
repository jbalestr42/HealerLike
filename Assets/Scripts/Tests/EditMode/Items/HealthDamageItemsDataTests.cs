using Entities;
using NUnit.Framework;
using UnityEditor;

namespace Items
{

// Current / Missing Health Damage and Strangle: hits deal extra damage from the health of the target, whatever
// the health of the attacker
public class HealthDamageItemsDataTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    static ConsumerFactory LoadConsumer(string name)
    {
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/EntityItems/" + name + "/" + name + ".asset");
        Assert.IsNotNull(item, name);
        Assert.AreEqual(1, item.data.onHitConsumers.Count, name);
        return (ConsumerFactory)item.data.onHitConsumers[0];
    }

    // Hit of an attacker at full health on a target at half health
    float Hit(ConsumerFactory consumer)
    {
        Entity attacker = _units.Create(100f, 100f, "Attacker");
        Entity target = _units.Create(500f, 1000f, "Target");

        target.health.AddResourceModifier(ResourceModifier.Create(consumer, attacker.gameObject, target.gameObject));
        TestUnits.Process(target.health);

        return 500f - target.health.Value;
    }

    [TestCase("CurrentHealthDamageItem")]
    [TestCase("MissingHealthDamageItem")]
    [TestCase("StrangleItem")]
    public void Consumer_IsComputedFromTheTarget(string name)
    {
        Assert.AreEqual(ConsumerValueOwner.Target, LoadConsumer(name).data.valueOwner);
    }

    [Test]
    public void CurrentHealthDamage_Deals10PercentOfTheTargetCurrentHealth()
    {
        Assert.AreEqual(50f, Hit(LoadConsumer("CurrentHealthDamageItem")), 0.0001f);
    }

    // The innate item of the Strangler Vine, half the Current Health Damage item
    [Test]
    public void Strangle_Deals5PercentOfTheTargetCurrentHealth()
    {
        Assert.AreEqual(25f, Hit(LoadConsumer("StrangleItem")), 0.0001f);
    }

    [Test]
    public void MissingHealthDamage_Deals10PercentOfTheTargetMissingHealth_EvenFromAnAttackerAtFullHealth()
    {
        Assert.AreEqual(50f, Hit(LoadConsumer("MissingHealthDamageItem")), 0.0001f);
    }
}

}
