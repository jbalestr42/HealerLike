using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// War Drums / Necronomicon: an item of the character gives a buff to the entities of a side. The
// battle start and the summons come from singletons in the game, the entities are given directly here
public class ApplyBuffOnEventBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    GameObject _owner;
    ApplyBuffOnEventBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Character");

        FlatModifierFactory damage = ScriptableObject.CreateInstance<FlatModifierFactory>();
        damage.data = new FlatModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Multiply, value = 0.5f };
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { damage } };
        _scriptableObjects.Add(damage);
        _scriptableObjects.Add(handler);

        _buff = new ApplyBuffOnEventBuff { data = new ApplyBuffOnEventBuffData { buffHandlerFactory = handler, entityType = Entity.EntityType.Player } };
        TestHelpers.SetPrivateField(_buff, "_owner", _owner);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_owner);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    Entity CreateUnit(Entity.EntityType entityType)
    {
        Entity unit = _units.Create(100f, 100f, entityType.ToString());
        unit.entityType = entityType;
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
    public void ApplyTo_AnEntityOfTheSide_GivesItTheBuff()
    {
        Entity ally = CreateUnit(Entity.EntityType.Player);

        _buff.ApplyTo(ally);

        Assert.AreEqual(15f, GetDamage(ally), 0.0001f);
    }

    [Test]
    public void ApplyTo_AnEntityOfTheOtherSide_GivesItNothing()
    {
        Entity enemy = CreateUnit(Entity.EntityType.Computer);

        _buff.ApplyTo(enemy);

        Assert.AreEqual(10f, GetDamage(enemy), 0.0001f);
    }

    [Test]
    public void ApplyTo_NoEntity_DoesNothing()
    {
        Assert.DoesNotThrow(() => _buff.ApplyTo(null));
    }
}

}
