using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Druid data: heals over time, Balance Life to share the health of the team, Verdant
// regenerating every unit in battle, and the Treant and Grove Keeper units
public class DruidDataTests
{
    CharacterData _druid;
    GameObject _healer;

    [SetUp]
    public void SetUp()
    {
        _druid = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/DruidCharacter/DruidCharacter.asset");
        Assert.IsNotNull(_druid);
        _healer = new GameObject("Healer");
        TestHelpers.CreateAttributeManager(_healer, AttributeType.HealPower, 20f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_healer);
    }

    T GetSkill<T>(string name) where T : ACharacterSkillFactory
    {
        ACharacterSkillFactory skill = _druid.skills.Find(s => s != null && s.Create().GetData().name == name);
        Assert.IsNotNull(skill, "The Druid has no " + name + " skill");
        Assert.IsInstanceOf<T>(skill, name);
        return (T)skill;
    }

    EntityData GetUnit(string title)
    {
        EntityData unit = _druid.entities.Find(entity => entity != null && entity.title == title);
        Assert.IsNotNull(unit, "The Druid can't recruit the " + title);
        return unit;
    }

    // A heal consumer gives a positive value, going through neither armor nor invincibility
    void AssertHeals(AConsumerFactory consumerFactory, float expected)
    {
        AConsumer consumer = consumerFactory.GetConsumer(_healer, _healer);
        Assert.AreEqual(expected, consumer.GetValue(), 0.0001f);
        Assert.IsTrue(consumer.ignoreConsumerPrevention);
        Assert.IsTrue(consumer.ignoreDamageReduction);
    }

    [Test]
    public void Druid_RecruitsOnlyUnitsFittingARegenerationTeam()
    {
        // The burst shooters (Multi Shot, Swarm) left the pool: nothing to do with a regeneration gameplay
        List<string> titles = _druid.entities.ConvertAll(entity => entity.title);

        CollectionAssert.AreEquivalent(new[] { "Normal", "Channeling", "Treant", "Grove Keeper" }, titles);
    }

    [Test]
    public void Druid_HasRejuvenationWildGrowthAndBalanceLife()
    {
        List<string> names = _druid.skills.ConvertAll(skill => skill.Create().GetData().name);

        CollectionAssert.AreEqual(new[] { "Rejuvenation", "Wild Growth", "Balance Life" }, names);
    }

    [Test]
    public void EveryDruidSkill_HasAnIconADescriptionACostAndACooldown()
    {
        foreach (ACharacterSkillFactory skill in _druid.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            Assert.IsNotNull(data.icon, data.name);
            Assert.IsFalse(string.IsNullOrEmpty(data.description), data.name);
            Assert.IsTrue(data.validators.Exists(v => v is ResourceValidatorFactory), data.name + " has no mana cost");
            Assert.IsTrue(data.validators.Exists(v => v is DurationValidatorFactory), data.name + " has no cooldown");
        }
    }

    [Test]
    public void Rejuvenation_HealsASingleAllyOverTimeWithoutStacking()
    {
        BuffCharacterSkillFactory rejuvenation = GetSkill<BuffCharacterSkillFactory>("Rejuvenation");

        Assert.IsTrue(rejuvenation.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, rejuvenation.data.entityType);
        BuffHandlerFactory handler = (BuffHandlerFactory)rejuvenation.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.data.durationType);
        Assert.IsTrue(handler.data.isPeriodic);
        Assert.AreEqual(1, handler.data.maxStacks);
        // 15% of the 20 Heal Power per tick
        AssertHeals(((ApplyConsumerBuffFactory)handler.data.buffFactoryList[0]).data.consumerFactory, 3f);
    }

    [Test]
    public void WildGrowth_HealsAllAlliesOverTime()
    {
        BuffCharacterSkillFactory wildGrowth = GetSkill<BuffCharacterSkillFactory>("Wild Growth");

        Assert.IsFalse(wildGrowth.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, wildGrowth.data.entityType);
        BuffHandlerFactory handler = (BuffHandlerFactory)wildGrowth.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.data.durationType);
        Assert.IsTrue(handler.data.isPeriodic);
        // 10% of the 20 Heal Power per tick
        AssertHeals(((ApplyConsumerBuffFactory)handler.data.buffFactoryList[0]).data.consumerFactory, 2f);
    }

    // The descriptions read the heal ratio from the data, so a balancing change can't leave them wrong
    [TestCase("Rejuvenation", "<color=\"green\">3</color> <color=#008080ff>(15% HealPower)</color>")]
    [TestCase("Wild Growth", "<color=\"green\">2</color> <color=#008080ff>(10% HealPower)</color>")]
    public void HotDescription_ShowsTheHealAndTheHealPowerPercentOfTheData(string name, string expected)
    {
        Character character = null;
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its attributes are read
        TestHelpers.WithLoggingDisabled(() => character = _healer.AddComponent<Character>());
        character.attributeManager = _healer.GetComponent<AttributeManager>();
        CharacterSkillData data = GetSkill<BuffCharacterSkillFactory>(name).Create().GetData();

        StringAssert.Contains(expected, TextConvertor.Convert(data.description, character, data));
    }

    [Test]
    public void BalanceLife_BalancesTheAllies()
    {
        BalanceLifeCharacterSkillFactory balance = GetSkill<BalanceLifeCharacterSkillFactory>("Balance Life");

        Assert.AreEqual(Entity.EntityType.Player, balance.data.entityType);
    }

    [Test]
    public void Druid_StartsWithVerdant_HealingEveryAllyPeriodically()
    {
        Assert.AreEqual(1, _druid.items.Count);
        ItemFactory verdant = _druid.items[0] as ItemFactory;
        Assert.IsNotNull(verdant);
        Assert.AreEqual("Verdant", verdant.data.name);
        Assert.IsNotNull(verdant.data.icon);

        BuffHandlerFactory handler = (BuffHandlerFactory)verdant.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.data.durationType);
        Assert.IsTrue(handler.data.isPeriodic);
        ApplyConsumerOnEntitiesBuffFactory buff = handler.data.buffFactoryList[0] as ApplyConsumerOnEntitiesBuffFactory;
        Assert.IsNotNull(buff);
        Assert.AreEqual(Entity.EntityType.Player, buff.data.entityType);
        // 5% of the 20 Heal Power
        AssertHeals(buff.data.consumerFactory, 1f);
    }

    [Test]
    public void Treant_IsA300HealthTankThatRegenerates()
    {
        EntityData treant = GetUnit("Treant");

        Assert.AreEqual(300f, treant.attributes[AttributeType.HealthMax]);
        ItemFactory sap = treant.items[0] as ItemFactory;
        Assert.IsNotNull(sap);
        BuffHandlerFactory handler = (BuffHandlerFactory)sap.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.data.durationType);
        Assert.IsTrue(handler.data.isPeriodic);
        Assert.Greater(((ApplyConsumerBuffFactory)handler.data.buffFactoryList[0]).data.consumerFactory.GetConsumer(_healer, _healer).GetValue(), 0f);
    }

    [Test]
    public void GroveKeeper_DoesNotAttackAndHealsItsMostWoundedAllyEvery4s()
    {
        EntityData keeper = GetUnit("Grove Keeper");

        Assert.AreEqual(0f, keeper.attributes[AttributeType.Damage]);
        // ApplyBuffOnTargetSkill always picks the ally with the lowest health
        Assert.AreEqual(1, keeper.skillFactories.Count);
        ApplyBuffOnTargetSkillFactory skill = keeper.skillFactories[0] as ApplyBuffOnTargetSkillFactory;
        Assert.IsNotNull(skill);
        Assert.IsTrue(skill.data.targetAlly);
        Assert.AreEqual(4f, skill.data.rate, 0.0001f);
        BuffHandlerFactory handler = (BuffHandlerFactory)skill.data.buffHandlerFactory;
        Assert.IsTrue(handler.data.isPeriodic);
        Assert.Greater(((ApplyConsumerBuffFactory)handler.data.buffFactoryList[0]).data.consumerFactory.GetConsumer(_healer, _healer).GetValue(), 0f);
    }
}

}
