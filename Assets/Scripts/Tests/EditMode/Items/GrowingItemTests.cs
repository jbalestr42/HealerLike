using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// Growing Seed: each battle its holder survives, the item stacks its growth buff once more on it. The
// growth belongs to the item. The end of the battles comes from AscensionGameType in the game, Grow() is
// called directly here
public class GrowingItemTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    Entity _first;
    Entity _second;
    GrowingItem _item;

    [SetUp]
    public void SetUp()
    {
        _first = CreateUnit("First");
        _second = CreateUnit("Second");

        // Each growth gives 1 more damage
        FlatModifierFactory damage = ScriptableObject.CreateInstance<FlatModifierFactory>();
        damage.data = new FlatModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 1f };
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { damage } };
        GrowingItemFactory factory = ScriptableObject.CreateInstance<GrowingItemFactory>();
        factory.data = new GrowingItemData { name = "Growing Seed", description = "Grows", growthBuffHandlerFactory = handler };
        _scriptableObjects.Add(damage);
        _scriptableObjects.Add(handler);
        _scriptableObjects.Add(factory);

        _item = (GrowingItem)factory.GetItem();
    }

    [TearDown]
    public void TearDown()
    {
        AscensionGameType.OnRoundEnd.RemoveListener(_item.Grow);
        _units.DestroyAll();
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    Entity CreateUnit(string name)
    {
        Entity unit = _units.Create(100f, 100f, name);
        unit.attributeManager = unit.GetComponent<AttributeManager>();
        unit.attributeManager.Add(AttributeType.Damage, new Attribute(10f));
        BuffManager buffManager = unit.GetComponent<BuffManager>();
        buffManager.isEnabled = true;
        TestHelpers.SetPrivateField(unit, "_buffManager", buffManager);
        return unit;
    }

    static float GetDamage(Entity unit)
    {
        unit.GetComponent<BuffManager>().ForceUpdate();
        Attribute damage = unit.attributeManager.Get(AttributeType.Damage);
        damage.Update();
        return damage.Value;
    }

    [Test]
    public void Equip_BeforeAnyBattle_GivesNothing()
    {
        _item.Equip(_first.gameObject);

        Assert.AreEqual(0, _item.growth);
        Assert.AreEqual(10f, GetDamage(_first), 0.0001f);
    }

    [Test]
    public void Grow_GivesTheGrowthToTheHolder()
    {
        _item.Equip(_first.gameObject);

        _item.Grow();

        Assert.AreEqual(1, _item.growth);
        Assert.AreEqual(11f, GetDamage(_first), 0.0001f);
    }

    [Test]
    public void Grow_EachBattle_AddsUp()
    {
        _item.Equip(_first.gameObject);

        _item.Grow();
        _item.Grow();
        _item.Grow();

        Assert.AreEqual(3, _item.growth);
        Assert.AreEqual(13f, GetDamage(_first), 0.0001f);
    }

    [Test]
    public void Unequip_TakesTheGrowthBackFromTheHolder()
    {
        _item.Equip(_first.gameObject);
        _item.Grow();
        _item.Grow();

        _item.Unequip(_first.gameObject);

        Assert.AreEqual(2, _item.growth);
        Assert.AreEqual(10f, GetDamage(_first), 0.0001f);
    }

    [Test]
    public void Equip_OnAnotherUnit_TakesTheGrowthAlong()
    {
        _item.Equip(_first.gameObject);
        _item.Grow();
        _item.Grow();
        GetDamage(_first);
        _item.Unequip(_first.gameObject);

        _item.Equip(_second.gameObject);

        Assert.AreEqual(10f, GetDamage(_first), 0.0001f);
        Assert.AreEqual(12f, GetDamage(_second), 0.0001f);
    }

    [Test]
    public void Grow_OnTheNewHolder_KeepsGrowingFromTheSameCount()
    {
        _item.Equip(_first.gameObject);
        _item.Grow();
        GetDamage(_first);
        _item.Unequip(_first.gameObject);
        _item.Equip(_second.gameObject);

        _item.Grow();

        Assert.AreEqual(2, _item.growth);
        Assert.AreEqual(12f, GetDamage(_second), 0.0001f);
    }

    [Test]
    public void Grow_AfterTheHolderDied_DoesNotGrow()
    {
        _item.Equip(_first.gameObject);
        _item.Grow();
        _units.DestroyAll();

        _item.Grow();

        Assert.AreEqual(1, _item.growth);
    }

    [Test]
    public void Description_ShowsTheGrowthOnceGrown()
    {
        Assert.AreEqual("Grows", _item.description);

        _item.Equip(_first.gameObject);
        _item.Grow();
        _item.Grow();

        StringAssert.StartsWith("Grows", _item.description);
        StringAssert.Contains("2", _item.description);
    }
}

}
