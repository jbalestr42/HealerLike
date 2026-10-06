using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Cleric data: classic healer with the strongest heals, a shield, a last resort
// invincibility, and units rewarding a well healed team: Zealot, Radiant Archer, Purifier, Paladin and Beacon
public class ClericDataTests
{
    const string CharactersPath = "Assets/Data/Characters/";

    CharacterData _cleric;

    [SetUp]
    public void SetUp()
    {
        _cleric = AssetDatabase.LoadAssetAtPath<CharacterData>(CharactersPath + "ClericCharacter/ClericCharacter.asset");
        Assert.IsNotNull(_cleric);
    }

    T GetSkill<T>(string name) where T : ACharacterSkillFactory
    {
        ACharacterSkillFactory skill = _cleric.skills.Find(s => s != null && s.Create().GetData().name == name);
        Assert.IsNotNull(skill, "The Cleric has no " + name + " skill");
        Assert.IsInstanceOf<T>(skill, name);
        return (T)skill;
    }

    float GetCooldown(CharacterSkillData data)
    {
        DurationValidatorFactory validator = (DurationValidatorFactory)data.validators.Find(v => v is DurationValidatorFactory);
        Assert.IsNotNull(validator, data.name + " has no cooldown");
        return validator.data.duration;
    }

    [Test]
    public void Cleric_HasTheHighestHealPower()
    {
        foreach (string other in new[] { "DruidCharacter", "WarlockCharacter" })
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(CharactersPath + other + "/" + other + ".asset");
            Assert.Greater(_cleric.attributes[AttributeType.HealPower], character.attributes[AttributeType.HealPower], other);
        }
    }

    [Test]
    public void Cleric_HasHealHealGroupShieldAndDivineIntervention()
    {
        List<string> names = _cleric.skills.ConvertAll(skill => skill.Create().GetData().name);

        CollectionAssert.AreEqual(new[] { "Heal", "Heal Group", "Shield", "Divine Intervention" }, names);
    }

    [Test]
    public void EveryClericSkill_HasAnIconAndACostAndACooldown()
    {
        foreach (ACharacterSkillFactory skill in _cleric.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            Assert.IsNotNull(data.icon, data.name);
            Assert.IsFalse(string.IsNullOrEmpty(data.description), data.name);
            Assert.IsTrue(data.validators.Exists(v => v is ResourceValidatorFactory), data.name + " has no mana cost");
            Assert.Greater(GetCooldown(data), 0f, data.name);
        }
    }

    static float GetCost(CharacterSkillData data)
    {
        ResourceValidatorFactory validator = (ResourceValidatorFactory)data.validators.Find(v => v is ResourceValidatorFactory);
        Assert.IsNotNull(validator, data.name + " has no mana cost");
        return validator.data.consumer.data.value.GetValue(null);
    }

    // Strong heals the Cleric must pay for: 120% Heal Power for 8 mana, 60% on every ally for 15 mana
    [TestCase("Heal", 1.2f, 8f)]
    [TestCase("Heal Group", 0.6f, 15f)]
    public void Heal_HealsTheRatioOfTheHealPower_ForItsCost(string name, float multiplier, float cost)
    {
        ApplyConsumerCharacterSkillFactory heal = GetSkill<ApplyConsumerCharacterSkillFactory>(name);

        Assert.AreEqual(multiplier, heal.data.multiplier, 0.0001f);
        Assert.AreEqual(cost, GetCost(heal.data), 0.0001f);
    }

    // The anticipation spell of the Cleric: cast on the unit about to take a big hit, percent armor only
    [Test]
    public void Shield_Gives70PercentArmorToASingleAllyFor5s()
    {
        BuffCharacterSkillFactory shield = GetSkill<BuffCharacterSkillFactory>("Shield");

        Assert.IsTrue(shield.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, shield.data.entityType);
        Assert.AreEqual(1, shield.data.buffHandlerFactory.Count);
        ABuffHandlerFactory handler = shield.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(5f, handler.duration, 0.0001f);
        Assert.AreEqual(1, handler.buffFactoryList.Count);

        FlatModifierFactory percentArmor = handler.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(percentArmor);
        Assert.AreEqual(AttributeType.PercentArmor, percentArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Add, percentArmor.data.modifierType);
        Assert.AreEqual(0.7f, percentArmor.data.value, 0.0001f);
    }

    [Test]
    public void Shield_TheDescriptionShowsThePercentArmorOnly()
    {
        CharacterSkillData data = GetSkill<BuffCharacterSkillFactory>("Shield").data;

        StringAssert.Contains("buffFactoryList|0.data.value", data.description);
        StringAssert.DoesNotContain("buffFactoryList|1", data.description);
        StringAssert.DoesNotContain("flat armor", data.description);
    }

    [Test]
    public void DivineIntervention_MakesAllAlliesInvincibleFor2s()
    {
        BuffCharacterSkillFactory divine = GetSkill<BuffCharacterSkillFactory>("Divine Intervention");

        Assert.IsFalse(divine.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, divine.data.entityType);
        ABuffHandlerFactory handler = divine.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(2f, handler.duration, 0.0001f);
        Assert.IsInstanceOf<InvincibilityBuffFactory>(handler.buffFactoryList[0]);
    }

    [Test]
    public void DivineIntervention_HasTheLongestCooldown()
    {
        float divineCooldown = GetCooldown(GetSkill<BuffCharacterSkillFactory>("Divine Intervention").data);

        foreach (ACharacterSkillFactory skill in _cleric.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            if (data.name != "Divine Intervention")
            {
                Assert.Greater(divineCooldown, GetCooldown(data), data.name);
            }
        }
    }

    // Invincibility blocks every consumer that doesn't ignore the prevention
    [Test]
    public void ClericHeals_StillHealInvincibleAllies()
    {
        GameObject healer = new GameObject("Healer");
        try
        {
            TestHelpers.CreateAttributeManager(healer, AttributeType.HealPower, 10f);
            foreach (string name in new[] { "Heal", "Heal Group" })
            {
                ApplyConsumerCharacterSkillFactory heal = GetSkill<ApplyConsumerCharacterSkillFactory>(name);
                Assert.IsTrue(heal.data.consumer.GetConsumer(healer, healer).ignoreConsumerPrevention, name);
            }
        }
        finally
        {
            Object.DestroyImmediate(healer);
        }
    }

    [Test]
    public void Cleric_StartsWithTheSacredTome()
    {
        ItemFactory sacredTome = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/PlayerItems/HealPowerItem/HealPowerItem.asset");

        CollectionAssert.AreEqual(new[] { sacredTome }, _cleric.items);
    }

    [Test]
    public void Cleric_StartsWithZealotRadiantArcherPurifierAndPaladin()
    {
        List<string> titles = _cleric.entities.ConvertAll(entity => entity.title);

        CollectionAssert.AreEqual(new[] { "Zealot", "Radiant Archer", "Purifier", "Paladin" }, titles);
    }

    // These units left the Cleric: they are no longer among its rewards
    [TestCase("NormalEntity/NormalEntity")]
    [TestCase("FastShootEntity/FastShootEntity")]
    [TestCase("BuffEntity/BuffEntity")]
    [TestCase("HitArmorBufferEntityEntity/HitArmorBufferEntity")]
    public void FormerClericUnit_HasNoClericTag(string path)
    {
        EntityData unit = LoadUnit(path);

        Assert.IsFalse(unit.HasTag(_cleric.classTag));
    }

    // Recruited along the run with the Cleric, each in its role
    [TestCase("RadiantArcherEntity/RadiantArcherEntity", TagNames.Damage)]
    [TestCase("PaladinEntity/PaladinEntity", TagNames.Tank)]
    [TestCase("PurifierEntity/PurifierEntity", TagNames.Support)]
    [TestCase("BeaconEntity/BeaconEntity", TagNames.Support)]
    public void NewClericUnit_IsAClericRewardInItsRole_InTheGameData(string path, string role)
    {
        EntityData unit = LoadUnit(path);
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset");

        Assert.IsTrue(unit.HasTag(_cleric.classTag));
        Assert.IsTrue(unit.HasTag(TagNames.Reward));
        Assert.IsTrue(unit.HasTag(role));
        CollectionAssert.Contains(data.entities, unit);
        Assert.IsFalse(string.IsNullOrEmpty(unit.description));
    }

    // Kept for later: a reward only
    [Test]
    public void Beacon_IsNotAStartingUnit()
    {
        CollectionAssert.DoesNotContain(_cleric.entities, LoadUnit("BeaconEntity/BeaconEntity"));
    }

    static EntityData LoadUnit(string path)
    {
        EntityData unit = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/" + path + ".asset");
        Assert.IsNotNull(unit, path);
        return unit;
    }

    // The buff its only item gives it, applied for the whole fight
    static T GetItemBuff<T>(EntityData unit) where T : ABuffFactory
    {
        Assert.AreEqual(1, unit.items.Count, unit.title);
        ItemFactory item = unit.items[0] as ItemFactory;
        Assert.IsNotNull(item, unit.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.name), unit.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.description), unit.title);
        ABuffHandlerFactory handler = item.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.durationType, unit.title);
        T buff = handler.buffFactoryList[0] as T;
        Assert.IsNotNull(buff, unit.title);
        return buff;
    }

    static void AssertModifier(ABuffFactory buff, AttributeType type, AttributeModifierType modifierType, float value)
    {
        FlatModifierFactory modifier = buff as FlatModifierFactory;
        Assert.IsNotNull(modifier, type.ToString());
        Assert.AreEqual(type, modifier.data.type);
        Assert.AreEqual(modifierType, modifier.data.modifierType, type.ToString());
        Assert.AreEqual(value, modifier.data.value, 0.0001f, type.ToString());
    }

    [Test]
    public void RadiantArcher_EachHealGivesPlus3DamageAndPlus50PercentAttackSpeedFor5s()
    {
        EntityData archer = LoadUnit("RadiantArcherEntity/RadiantArcherEntity");
        Assert.AreEqual(5f, archer.attributes[AttributeType.Damage]);
        // An attack every 0.9s
        Assert.AreEqual(0.9f, archer.attributes[AttributeType.AttackRate], 0.0001f);
        Assert.AreEqual(1, archer.skillFactories.Count, "it attacks");

        ABuffHandlerFactory radiance = GetItemBuff<ApplyBuffOnHealBuffFactory>(archer).data.buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, radiance.durationType);
        Assert.AreEqual(5f, radiance.duration, 0.0001f);
        Assert.AreEqual(1, radiance.maxStacks);
        Assert.AreEqual(2, radiance.buffFactoryList.Count);
        AssertModifier(radiance.buffFactoryList[0], AttributeType.Damage, AttributeModifierType.Add, 3f);
        // The attack cooldown x0.67: 1.5 times as many attacks
        AssertModifier(radiance.buffFactoryList[1], AttributeType.AttackRate, AttributeModifierType.Multiply, -0.33f);
        Assert.IsEmpty(radiance.tags, "removed at the end of the fight");
    }

    [Test]
    public void Paladin_EachHealGivesPlus1ArmorFor10s_UpTo3()
    {
        EntityData paladin = LoadUnit("PaladinEntity/PaladinEntity");
        Assert.AreEqual(200f, paladin.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(1, paladin.skillFactories.Count, "it attacks");

        ABuffHandlerFactory devotion = GetItemBuff<ApplyBuffOnHealBuffFactory>(paladin).data.buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, devotion.durationType);
        Assert.AreEqual(10f, devotion.duration, 0.0001f);
        Assert.AreEqual(3, devotion.maxStacks);
        Assert.AreEqual(1, devotion.buffFactoryList.Count);
        AssertModifier(devotion.buffFactoryList[0], AttributeType.FlatArmor, AttributeModifierType.Add, 1f);
        Assert.IsEmpty(devotion.tags, "removed at the end of the fight");
    }

    [Test]
    public void Purifier_Every5s_RemovesADebuffAndGivesAttackSpeedDamageOrArmorFor5s()
    {
        EntityData purifier = LoadUnit("PurifierEntity/PurifierEntity");
        Assert.AreEqual(1, purifier.skillFactories.Count, "no attack, only its purify");

        PurifySkillFactory purify = purifier.skillFactories[0] as PurifySkillFactory;

        Assert.IsNotNull(purify);
        Assert.AreEqual(5f, purify.data.rate, 0.0001f);
        Assert.IsNotNull(purify.data.debuffTag);
        Assert.AreEqual(TagNames.Debuff, purify.data.debuffTag.name);
        Assert.AreEqual(3, purify.data.buffHandlerFactories.Count);
        foreach (ABuffHandlerFactory blessing in purify.data.buffHandlerFactories)
        {
            Assert.AreEqual(DurationType.Duration, blessing.durationType, blessing.name);
            Assert.AreEqual(5f, blessing.duration, 0.0001f, blessing.name);
            Assert.AreEqual(1, blessing.maxStacks, blessing.name);
            Assert.IsNotNull(blessing.icon, blessing.name);
        }
        AssertModifier(purify.data.buffHandlerFactories[0].buffFactoryList[0], AttributeType.AttackRate, AttributeModifierType.Multiply, -0.33f);
        AssertModifier(purify.data.buffHandlerFactories[1].buffFactoryList[0], AttributeType.Damage, AttributeModifierType.Add, 3f);
        AssertModifier(purify.data.buffHandlerFactories[2].buffFactoryList[0], AttributeType.FlatArmor, AttributeModifierType.Add, 2f);
    }

    [Test]
    public void Beacon_CopiesTheHealsOfTheCharacterFor40PercentOnItsAdjacentAllies()
    {
        EntityData beacon = LoadUnit("BeaconEntity/BeaconEntity");
        Assert.IsEmpty(beacon.skillFactories, "no attack");

        ShareCharacterHealBuffFactory share = GetItemBuff<ShareCharacterHealBuffFactory>(beacon);

        Assert.AreEqual(0.4f, share.data.ratio, 0.0001f);
        Assert.AreEqual(RelativeCellPatternType.Adjacent, share.data.pattern);
        Assert.AreEqual(1, share.data.range);
    }

    // What the Purifier removes: the harmful buffs the enemies put on the units
    [TestCase("EntityItems/VenomItem/BuffHandlerFactory")]
    [TestCase("EntityItems/PlagueItem/BuffHandlerFactory")]
    [TestCase("EntityItems/FrostItem/BuffHandlerFactory")]
    [TestCase("Entities/HexerEntity/BuffHandlerFactory")]
    public void EnemyDebuff_HasTheDebuffTag(string path)
    {
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>("Assets/Data/" + path + ".asset");

        Assert.IsNotNull(handler, path);
        Assert.IsTrue(handler.HasTag(TagNames.Debuff), path);
    }

    [Test]
    public void Cleric_RecruitsTheZealot()
    {
        Assert.IsTrue(_cleric.entities.Exists(entity => entity != null && entity.title == "Zealot"));
    }

    [Test]
    public void Zealot_Deals50PercentMoreDamageAbove70PercentHealth()
    {
        EntityData zealot = _cleric.entities.Find(entity => entity != null && entity.title == "Zealot");
        Assert.IsNotNull(zealot);
        Assert.AreEqual(1, zealot.items.Count);

        ItemFactory zeal = zealot.items[0] as ItemFactory;
        Assert.IsNotNull(zeal);
        ABuffHandlerFactory handler = zeal.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.durationType);

        HealthThresholdModifierFactory modifier = handler.buffFactoryList[0] as HealthThresholdModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.Damage, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, modifier.data.modifierType);
        Assert.AreEqual(0.5f, modifier.data.value, 0.0001f);
        Assert.AreEqual(0.7f, modifier.data.threshold, 0.0001f);
    }
}

}
