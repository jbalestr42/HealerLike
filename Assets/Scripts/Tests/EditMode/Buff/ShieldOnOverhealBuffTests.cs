using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Relic of Light: the heals the units receive above their max health become a shield on them.
// The units come from the EntityManager singleton in the game, they're listened to directly here
public class ShieldOnOverhealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    GameObject _characterGo;
    ShieldOnOverhealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        _objects.Add(_characterGo);
        _buff = new ShieldOnOverhealBuff { data = new ShieldOnOverhealBuffData { ratio = 1f, maxHealthRatio = 0.2f, healedType = Entity.EntityType.Player } };
        TestHelpers.SetPrivateField(_buff, "_owner", _characterGo);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    // A unit of the side with 100 max health and an empty shield
    Entity Create(Entity.EntityType entityType)
    {
        Entity entity = _units.Create(100f, 100f, entityType.ToString());
        entity.entityType = entityType;
        entity.GetComponent<BuffManager>().isEnabled = true;
        GameObject shieldGo = new GameObject("Shield");
        _objects.Add(shieldGo);
        ResourceAttribute shield = TestHelpers.CreateResourceAttribute(shieldGo, ResourceType.Shield, AttributeType.HealthMax, 100f);
        shield.fillWithMax = false;
        shield.SetValue(0f);
        TestHelpers.SetPrivateField(entity, "_shield", shield);
        return entity;
    }

    // A heal of 40 of which the overheal is above the max health of the healed unit
    static float ShieldAfterHeal(Entity healed, float overheal)
    {
        Entity.NotifyHealed(null, healed.gameObject, new ConsumerResult(40f, false, overheal));
        TestUnits.Process(healed.shield);
        return healed.shield.Value;
    }

    [Test]
    public void Overheal_OfAListenedAlly_BecomesItsShield()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Assert.AreEqual(12f, ShieldAfterHeal(ally, 12f), 0.0001f);
    }

    // 20% of its max health of 100
    [Test]
    public void Overheal_AddsUp_UpToItsPartOfTheMaxHealth()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        ShieldAfterHeal(ally, 12f);

        Assert.AreEqual(20f, ShieldAfterHeal(ally, 12f), 0.0001f);
    }

    // Two overheals of the same frame both count
    [Test]
    public void Overheal_SeveralInAFrame_AddUp()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 5f));
        Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 7f));
        TestUnits.Process(ally.shield);

        Assert.AreEqual(12f, ally.shield.Value, 0.0001f);
    }

    // The overheal is already final: never a critical again
    [Test]
    public void Overheal_GivenThroughAConsumer_NeverCritical()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        _buff.AddShield(ally, new ConsumerResult(40f, false, 12f));

        ResourceModifier modifier = TestHelpers.GetPrivateField<List<ResourceModifier>>(ally.shield, "_resourceModifiers")[0];
        Assert.AreSame(_characterGo, modifier.source);
        Assert.IsFalse(modifier.consumers[0].canBeCritical);
    }

    [Test]
    public void Overheal_TheRatioOfIt()
    {
        _buff.data.ratio = 0.5f;
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Assert.AreEqual(5f, ShieldAfterHeal(ally, 10f), 0.0001f);
    }

    [Test]
    public void Heal_WithoutOverheal_NoShield()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Assert.AreEqual(0f, ShieldAfterHeal(ally, 0f));
    }

    // Only the healed unit, not the others
    [Test]
    public void Overheal_OnlyOnTheHealedUnit()
    {
        Entity ally = Create(Entity.EntityType.Player);
        Entity other = Create(Entity.EntityType.Player);
        _buff.Listen(ally);
        _buff.Listen(other);

        ShieldAfterHeal(ally, 12f);
        TestUnits.Process(other.shield);

        Assert.AreEqual(0f, other.shield.Value);
    }

    [Test]
    public void Listen_AnEnemy_IsIgnored()
    {
        Entity enemy = Create(Entity.EntityType.Computer);
        _buff.Listen(enemy);

        Assert.AreEqual(0f, ShieldAfterHeal(enemy, 12f));
    }

    [Test]
    public void StopListening_NoMoreShield()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        _buff.StopListening();

        Assert.AreEqual(0f, ShieldAfterHeal(ally, 12f));
    }

    // Two overheals of the same frame stop together at the limit
    [Test]
    public void Overheal_SeveralInAFrame_StopAtItsPartOfTheMaxHealth()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 15f));
        Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 15f));
        TestUnits.Process(ally.shield);

        Assert.AreEqual(20f, ally.shield.Value, 0.0001f);
    }

    // The limit is its own: another shield source may go further, up to the max health
    [Test]
    public void Limit_OnlyItsOwnGains()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);
        ResourceModifier other = new ResourceModifier { source = null };
        other.consumers.Add(new RuntimeConsumer(50f, canBeCritical: false));
        ally.shield.AddResourceModifier(other);

        TestUnits.Process(ally.shield);

        Assert.AreEqual(50f, ally.shield.Value, 0.0001f);
    }

    // Already above its limit (e.g. another source): it gives nothing more, never takes some back
    [Test]
    public void Overheal_AlreadyAboveTheLimit_NothingMore()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);
        ally.shield.SetValue(30f);

        Assert.AreEqual(30f, ShieldAfterHeal(ally, 12f), 0.0001f);
    }

    [Test]
    public void Listen_AUnitWithoutShield_IsIgnored()
    {
        Entity ally = _units.Create(100f, 100f, "NoShield");
        ally.entityType = Entity.EntityType.Player;

        Assert.DoesNotThrow(() => _buff.Listen(ally));
        Assert.DoesNotThrow(() => Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 12f)));
    }
}

}
