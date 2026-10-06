using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Cleric data: classic healer with the strongest heals, a shield, a last resort
// invincibility and the Zealot, a unit rewarding a well healed team
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

    // The anticipation spell of the Cleric: cast on the unit about to take a big hit
    [Test]
    public void Shield_Gives70PercentArmorAndFlatArmorToASingleAllyFor5s()
    {
        BuffCharacterSkillFactory shield = GetSkill<BuffCharacterSkillFactory>("Shield");

        Assert.IsTrue(shield.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, shield.data.entityType);
        Assert.AreEqual(1, shield.data.buffHandlerFactory.Count);
        ABuffHandlerFactory handler = shield.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(5f, handler.duration, 0.0001f);
        Assert.AreEqual(2, handler.buffFactoryList.Count);

        FlatModifierFactory percentArmor = handler.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(percentArmor);
        Assert.AreEqual(AttributeType.PercentArmor, percentArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Add, percentArmor.data.modifierType);
        Assert.AreEqual(0.7f, percentArmor.data.value, 0.0001f);

        FlatModifierFactory flatArmor = handler.buffFactoryList[1] as FlatModifierFactory;
        Assert.IsNotNull(flatArmor);
        Assert.AreEqual(AttributeType.FlatArmor, flatArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Add, flatArmor.data.modifierType);
        Assert.AreEqual(5f, flatArmor.data.value, 0.0001f);
    }

    [Test]
    public void Shield_TheDescriptionShowsBothArmors()
    {
        CharacterSkillData data = GetSkill<BuffCharacterSkillFactory>("Shield").data;

        StringAssert.Contains("buffFactoryList|0.data.value", data.description);
        StringAssert.Contains("buffFactoryList|1.data.value", data.description);
        StringAssert.Contains("flat armor", data.description);
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
    public void Cleric_StartsWithFastShotBuffHitArmorBufferAndZealot()
    {
        List<string> titles = _cleric.entities.ConvertAll(entity => entity.title);

        CollectionAssert.AreEqual(new[] { "Fast Shot", "Buff", "Hit Armor Buffer", "Zealot" }, titles);
    }

    // Normal left the Cleric: it is no longer among its rewards
    [Test]
    public void Normal_HasNoClericTag()
    {
        EntityData normal = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/NormalEntity/NormalEntity.asset");

        Assert.IsNotNull(normal);
        Assert.IsFalse(normal.HasTag(_cleric.classTag));
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
