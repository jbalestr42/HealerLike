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

    // Hungering Mask: +1 damage per kill and -10 max health per battle here, each up to its limit
    GrowingItem CreateHungeringItem(int maxGrowth, int maxKillGrowth)
    {
        FlatModifierFactory damage = ScriptableObject.CreateInstance<FlatModifierFactory>();
        damage.data = new FlatModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 1f };
        BuffHandlerFactory killHandler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        killHandler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { damage } };
        FlatModifierFactory health = ScriptableObject.CreateInstance<FlatModifierFactory>();
        health.data = new FlatModifierData { type = AttributeType.HealthMax, modifierType = AttributeModifierType.Add, value = -10f };
        BuffHandlerFactory battleHandler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        battleHandler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { health } };
        GrowingItemFactory factory = ScriptableObject.CreateInstance<GrowingItemFactory>();
        factory.data = new GrowingItemData
        {
            name = "Hungering Mask",
            description = "Hungers",
            growthBuffHandlerFactory = battleHandler,
            maxGrowth = maxGrowth,
            killGrowthBuffHandlerFactory = killHandler,
            maxKillGrowth = maxKillGrowth,
        };
        _scriptableObjects.AddRange(new Object[] { damage, killHandler, health, battleHandler, factory });
        return (GrowingItem)factory.GetItem();
    }

    static float GetMaxHealth(Entity unit)
    {
        unit.GetComponent<BuffManager>().ForceUpdate();
        Attribute health = unit.attributeManager.GetOrAdd(AttributeType.HealthMax);
        health.Update();
        return health.Value;
    }

    [Test]
    public void Kill_GrowsTheKillGrowth()
    {
        GrowingItem mask = CreateHungeringItem(0, 0);
        mask.Equip(_first.gameObject);

        _first.OnKill.Invoke(_second);
        _first.OnKill.Invoke(_second);

        Assert.AreEqual(2, mask.killGrowth);
        Assert.AreEqual(0, mask.growth);
        Assert.AreEqual(12f, GetDamage(_first), 0.0001f);
        mask.Unequip(_first.gameObject);
    }

    [Test]
    public void Kill_StopsAtItsLimit()
    {
        GrowingItem mask = CreateHungeringItem(0, 2);
        mask.Equip(_first.gameObject);

        for (int i = 0; i < 5; i++)
        {
            _first.OnKill.Invoke(_second);
        }

        Assert.AreEqual(2, mask.killGrowth);
        Assert.AreEqual(12f, GetDamage(_first), 0.0001f);
        mask.Unequip(_first.gameObject);
    }

    [Test]
    public void Grow_StopsAtItsLimit()
    {
        GrowingItem mask = CreateHungeringItem(3, 0);
        _first.attributeManager.Add(AttributeType.HealthMax, new Attribute(100f));
        mask.Equip(_first.gameObject);

        for (int i = 0; i < 5; i++)
        {
            mask.Grow();
        }

        Assert.AreEqual(3, mask.growth);
        Assert.AreEqual(70f, GetMaxHealth(_first), 0.0001f);
        mask.Unequip(_first.gameObject);
    }

    [Test]
    public void Kill_AfterUnequip_DoesNotGrow()
    {
        GrowingItem mask = CreateHungeringItem(0, 0);
        mask.Equip(_first.gameObject);
        mask.Unequip(_first.gameObject);

        _first.OnKill.Invoke(_second);

        Assert.AreEqual(0, mask.killGrowth);
    }

    [Test]
    public void KillGrowth_MovesWithTheItem()
    {
        GrowingItem mask = CreateHungeringItem(0, 0);
        mask.Equip(_first.gameObject);
        _first.OnKill.Invoke(_second);
        _first.OnKill.Invoke(_second);
        GetDamage(_first);
        mask.Unequip(_first.gameObject);

        mask.Equip(_second.gameObject);

        Assert.AreEqual(10f, GetDamage(_first), 0.0001f);
        Assert.AreEqual(12f, GetDamage(_second), 0.0001f);
        mask.Unequip(_second.gameObject);
    }

    [Test]
    public void Description_WithKillGrowth_ShowsBothCounts()
    {
        GrowingItem mask = CreateHungeringItem(0, 0);
        Assert.AreEqual("Hungers", mask.description);

        mask.Equip(_first.gameObject);
        _first.OnKill.Invoke(_second);
        mask.Grow();

        Assert.AreEqual("Hungers (1 kills, 1 battles)", mask.description);
        mask.Unequip(_first.gameObject);
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
