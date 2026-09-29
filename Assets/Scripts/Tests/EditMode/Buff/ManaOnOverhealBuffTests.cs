using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Overflowing Font: the character gets mana from the heals its entities receive above their max health.
// The entities come from the EntityManager singleton in the game, they're listened to directly here
public class ManaOnOverhealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _characterGo;
    GameObject _manaGo;
    Character _character;
    ManaOnOverhealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        TestHelpers.CreateAttributeManager(_characterGo);
        // Adding Character triggers its editor-only Reset(), which NREs without Init()
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());
        _manaGo = new GameObject("Mana");
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_manaGo, AttributeType.ManaMax, 100f);
        mana.SetValue(50f);
        TestHelpers.SetPrivateField(_character, "_mana", mana);

        _buff = new ManaOnOverhealBuff { data = new ManaOnOverhealBuffData { ratio = 0.1f, healedType = Entity.EntityType.Player } };
        TestHelpers.SetPrivateField(_buff, "_owner", _character);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_manaGo);
    }

    Entity Create(Entity.EntityType entityType)
    {
        Entity entity = _units.Create(100f, 100f, entityType.ToString());
        entity.entityType = entityType;
        return entity;
    }

    // A heal of 40 of which the overheal is above the max health of the healed entity
    float ManaAfterHeal(Entity healed, float overheal)
    {
        Entity.NotifyHealed(null, healed.gameObject, new ConsumerResult(40f, false, overheal));
        TestUnits.Process(_character.mana);
        return _character.mana.Value;
    }

    [Test]
    public void Overheal_OfAListenedAlly_RestoresTheRatioAsMana()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Assert.AreEqual(53f, ManaAfterHeal(ally, 30f), 0.0001f);
    }

    [Test]
    public void Heal_WithoutOverheal_RestoresNothing()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Assert.AreEqual(50f, ManaAfterHeal(ally, 0f), 0.0001f);
    }

    [Test]
    public void Listen_AnEnemy_IsIgnored()
    {
        Entity enemy = Create(Entity.EntityType.Computer);
        _buff.Listen(enemy);

        Assert.AreEqual(50f, ManaAfterHeal(enemy, 30f), 0.0001f);
    }

    [Test]
    public void Listen_TheSameAllyTwice_RestoresManaOnce()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);
        _buff.Listen(ally);

        Assert.AreEqual(53f, ManaAfterHeal(ally, 30f), 0.0001f);
    }

    [Test]
    public void Overheal_AfterStopListening_RestoresNothing()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);
        _buff.StopListening();

        Assert.AreEqual(50f, ManaAfterHeal(ally, 30f), 0.0001f);
    }

    [Test]
    public void Overheal_ManaComesFromTheCharacter()
    {
        Entity ally = Create(Entity.EntityType.Player);
        _buff.Listen(ally);

        Entity.NotifyHealed(null, ally.gameObject, new ConsumerResult(40f, false, 30f));

        Assert.AreSame(_characterGo, TestHelpers.GetPrivateField<List<ResourceModifier>>(_character.mana, "_resourceModifiers")[0].source);
    }
}

}
