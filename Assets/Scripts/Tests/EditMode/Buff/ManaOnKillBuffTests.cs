using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Tithe: the character gets mana each time an entity of the killed side dies. The kills come from
// the EntityManager singleton in the game, they're reported directly here
public class ManaOnKillBuffTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _characterGo;
    GameObject _manaGo;
    Character _character;
    ConsumerFactory _manaGain;
    ManaOnKillBuff _buff;

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

        _manaGain = ScriptableObject.CreateInstance<ConsumerFactory>();
        // The consumer removes its value from the resource: a negative one restores it
        _manaGain.data = new ConsumerData { ignoreDamageReduction = true, value = new FlatValue { data = new FlatValueData { value = -3f } } };
        _buff = new ManaOnKillBuff { data = new ManaOnKillBuffData { consumerFactory = _manaGain, killedType = Entity.EntityType.Computer } };
        TestHelpers.SetPrivateField(_buff, "_owner", _character);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_manaGo);
        Object.DestroyImmediate(_manaGain);
    }

    Entity CreateDead(Entity.EntityType entityType)
    {
        Entity dead = _units.Create(0f, 100f, "Dead");
        dead.entityType = entityType;
        return dead;
    }

    void Kill(Entity.EntityType entityType)
    {
        _buff.OnEntityKilled(CreateDead(entityType));
        TestUnits.Process(_character.mana);
    }

    [Test]
    public void EnemyKilled_RestoresTheConsumerValueToTheMana()
    {
        Kill(Entity.EntityType.Computer);

        Assert.AreEqual(53f, _character.mana.Value, 0.0001f);
    }

    [Test]
    public void AllyKilled_RestoresNothing()
    {
        Kill(Entity.EntityType.Player);

        Assert.AreEqual(50f, _character.mana.Value, 0.0001f);
    }

    [Test]
    public void Stack_MultipliesTheManaByTheStacks()
    {
        _buff.Stack(_characterGo, _characterGo);

        Kill(Entity.EntityType.Computer);

        Assert.AreEqual(56f, _character.mana.Value, 0.0001f);
    }

    [Test]
    public void Unstack_ReducesTheManaBackToOneStack()
    {
        _buff.Stack(_characterGo, _characterGo);
        _buff.Unstack(_characterGo, _characterGo);

        Kill(Entity.EntityType.Computer);

        Assert.AreEqual(53f, _character.mana.Value, 0.0001f);
    }
}

}
