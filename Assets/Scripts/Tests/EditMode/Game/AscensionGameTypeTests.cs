using NUnit.Framework;
using UnityEngine;

namespace Game
{

// Only the pieces independent from the singletons (GameManager, UIManager, ...) the run loop relies on
public class AscensionGameTypeTests
{
    GameObject _entityGo;
    GameObject _healthGo;
    Entity _entity;
    ResourceAttribute _health;
    ConsumerFactory _restHeal;

    [SetUp]
    public void SetUp()
    {
        _entityGo = new GameObject();
        // Adding Entity triggers Entity.Reset() (an editor-only message), which NREs without a
        // full Entity.Init() - not needed here, we only use it as a holder for .health.
        TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());

        _healthGo = new GameObject();
        _health = TestHelpers.CreateResourceAttribute(_healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(_entity, "_health", _health);

        // Same setup as the rest room asset: heal 30% of the max health, never reduced nor prevented
        _restHeal = ScriptableObject.CreateInstance<ConsumerFactory>();
        _restHeal.data = new ConsumerData
        {
            ignoreDamageReduction = true,
            ignoreConsumerPrevention = true,
            value = new MaxHealthValue { data = new MaxHealthValueData { multiplier = -0.3f } },
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_entityGo);
        Object.DestroyImmediate(_healthGo);
        Object.DestroyImmediate(_restHeal);
    }

    void SetHealth(float value)
    {
        TestHelpers.SetPrivateField(_health, "_value", value);
    }

    void Drain()
    {
        TestHelpers.InvokePrivate(_health, "Update");
    }

    [Test]
    public void ApplyConsumer_RestHeal_GivesBackAShareOfTheMaxHealth()
    {
        SetHealth(40f);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(70f, _health.Value, 0.001f);
    }

    [Test]
    public void ApplyConsumer_RestHeal_NeverHealsAboveMax()
    {
        SetHealth(90f);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void ApplyConsumer_GoesThroughTheResourceFlowWithTheEntityAsSource()
    {
        SetHealth(40f);
        GameObject processedTarget = null;
        ResourceModifier processedModifier = null;
        float processedValue = 0f;
        _health.OnAllConsumerProcessed.AddListener((target, modifier, result) =>
        {
            processedTarget = target;
            processedModifier = modifier;
            processedValue = result.value;
        });
        int changedCount = 0;
        _health.OnValueChanged.AddListener(_ => changedCount++);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreSame(_healthGo, processedTarget);
        Assert.AreSame(_entityGo, processedModifier.source);
        Assert.AreEqual(30f, processedValue, 0.001f);
        Assert.AreEqual(1, changedCount);
    }

    [Test]
    public void ApplyConsumer_RestHeal_IsNotBlockedByConsumerPrevention()
    {
        SetHealth(40f);
        _health.preventConsumers = true;

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(70f, _health.Value, 0.001f);
    }

    // A character holding only an attribute manager, with the given RewardChoices (none when null)
    static GameObject CreateCharacter(float? rewardChoices)
    {
        GameObject character = new GameObject("Character");
        AttributeManager attributes = TestHelpers.CreateAttributeManager(character);
        if (rewardChoices.HasValue)
        {
            attributes.Add(AttributeType.RewardChoices, new Attribute(rewardChoices.Value));
        }
        return character;
    }

    [Test]
    public void GetRewardChoiceCount_WithoutRewardChoices_IsTheRoomCount()
    {
        GameObject character = CreateCharacter(null);

        Assert.AreEqual(3, AscensionGameType.GetRewardChoiceCount(3, character));
        Assert.AreEqual(3, AscensionGameType.GetRewardChoiceCount(3, null));
        Object.DestroyImmediate(character);
    }

    [Test]
    public void GetRewardChoiceCount_WithRewardChoices_AddsThem()
    {
        // e.g. two Merchant's Ledgers in an elite room
        GameObject character = CreateCharacter(2f);

        Assert.AreEqual(6, AscensionGameType.GetRewardChoiceCount(4, character));
        Object.DestroyImmediate(character);
    }

    [Test]
    public void IsLostForTheRun_OnlyTheAlliesThatAreNotSummons()
    {
        GameObject allyGo = new GameObject();
        GameObject summonGo = new GameObject();
        GameObject enemyGo = new GameObject();
        GameplayTag summonTag = ScriptableObject.CreateInstance<GameplayTag>();
        try
        {
            Entity ally = null;
            Entity summon = null;
            Entity enemy = null;
            TestHelpers.WithLoggingDisabled(() =>
            {
                ally = allyGo.AddComponent<Entity>();
                summon = summonGo.AddComponent<Entity>();
                enemy = enemyGo.AddComponent<Entity>();
            });
            ally.entityType = Entity.EntityType.Player;
            summon.entityType = Entity.EntityType.Player;
            summon.AddTag(summonTag);
            enemy.entityType = Entity.EntityType.Computer;

            Assert.IsTrue(AscensionGameType.IsLostForTheRun(ally, summonTag));
            Assert.IsFalse(AscensionGameType.IsLostForTheRun(summon, summonTag));
            Assert.IsFalse(AscensionGameType.IsLostForTheRun(enemy, summonTag));
            Assert.IsFalse(AscensionGameType.IsLostForTheRun(null, summonTag));
        }
        finally
        {
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(summonGo);
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(summonTag);
        }
    }

    // A character holding only its mana
    ResourceAttribute CreateMana(GameObject go, float max, float value)
    {
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(go, AttributeType.ManaMax, max);
        mana.SetValue(value);
        TestHelpers.InvokePrivate(mana, "Update");
        return mana;
    }

    [Test]
    public void RefillManaForCombat_ManaSpentInThePreviousFight_Gives40PercentOfTheMaxBack()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(55f, mana.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void RefillManaForCombat_CombatManaRefillAttribute_GivesThatShareBack()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);
            characterGo.GetComponent<AttributeManager>().Add(AttributeType.CombatManaRefill, new Attribute(0.25f));

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(40f, mana.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void RefillManaForCombat_CharacterWithFullCriticalChance_IsNeverCritical()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);
            characterGo.GetComponent<AttributeManager>().Add(AttributeType.CriticalChance, new Attribute(100f));

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(55f, mana.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void RefillManaForCombat_NotifiesTheGainLikeAnyOther()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);
            float notifiedGain = 0f;
            mana.OnAllConsumerProcessed.AddListener((target, modifier, result) => notifiedGain += result.value);

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(40f, notifiedGain, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void CombatManaRefill_ByDefault_Is40Percent()
    {
        Assert.AreEqual(0.4f, AttributeManager.GetDefaultValue(AttributeType.CombatManaRefill), 0.0001f);
    }

    [Test]
    public void RefillManaForCombat_ManaAlmostFull_IsCappedByTheMax()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 80f);

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(100f, mana.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void RefillManaForCombat_MaxRaisedByAnItem_Gives40PercentOfTheNewMaxBack()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);
            Attribute manaMax = characterGo.GetComponent<AttributeManager>().Get(AttributeType.ManaMax);
            manaMax.AddModifier(AttributeModifierType.Add, characterGo, new FakeModifier(50f));
            manaMax.Update();

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");

            // The raised max already gave its +50 (15 -> 65), then 40% of 150 comes back
            Assert.AreEqual(125f, mana.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }

    [Test]
    public void RefillManaForCombat_UnitsKeepTheirMissingHealth()
    {
        GameObject characterGo = new GameObject();
        try
        {
            ResourceAttribute mana = CreateMana(characterGo, 100f, 15f);
            _health.SetValue(40f);
            TestHelpers.InvokePrivate(_health, "Update");

            AscensionGameType.RefillManaForCombat(mana);
            TestHelpers.InvokePrivate(mana, "Update");
            TestHelpers.InvokePrivate(_health, "Update");

            Assert.AreEqual(40f, _health.Value, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(characterGo);
        }
    }
}

}
