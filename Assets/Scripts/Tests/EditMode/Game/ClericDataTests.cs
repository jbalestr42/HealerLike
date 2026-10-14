using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Cleric data: the strongest heals, cheap and frequent, building Devotion that the Shield and Judgment
// spend, a last resort invincibility, and units rewarding a well healed team: Radiant Archer and Paladin at the start,
// Zealot, Purifier and Beacon recruited
public class ClericDataTests
{
    const string CharactersPath = "Assets/Data/Characters/";
    const string BlessedStatusPath = "Assets/Data/Statuses/BlessedStatus/BlessedStatus.asset";

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
    public void Cleric_HasHealMendingLightShieldJudgmentAndDivineIntervention()
    {
        List<string> names = _cleric.skills.ConvertAll(skill => skill.Create().GetData().name);

        CollectionAssert.AreEqual(new[] { "Heal", "Mending Light", "Shield", "Judgment", "Divine Intervention" }, names);
    }

    [Test]
    public void EveryClericSkill_HasAnIconAndACostAndACooldown()
    {
        foreach (ACharacterSkillFactory skill in _cleric.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            Assert.IsNotNull(data.icon, data.name);
            Assert.IsFalse(string.IsNullOrEmpty(data.description), data.name);
            Assert.IsTrue(data.validators.Exists(v => v is ResourceValidatorFactory r && r.data.resourceType == ResourceType.Mana), data.name + " has no mana cost");
            Assert.Greater(GetCooldown(data), 0f, data.name);
        }
    }

    static float GetCost(CharacterSkillData data)
    {
        ResourceValidatorFactory validator = (ResourceValidatorFactory)data.validators.Find(v => v is ResourceValidatorFactory r && r.data.resourceType == ResourceType.Mana);
        Assert.IsNotNull(validator, data.name + " has no mana cost");
        return validator.data.consumer.data.value.GetValue(null);
    }

    // Cheap and frequent, weaker than before the Devotion
    [Test]
    public void Heal_Heals50PercentOfTheHealPower_For5Mana()
    {
        ApplyConsumerCharacterSkillFactory heal = GetSkill<ApplyConsumerCharacterSkillFactory>("Heal");

        Assert.AreEqual(0.5f, heal.data.multiplier, 0.0001f);
        Assert.AreEqual(5f, GetCost(heal.data), 0.0001f);
    }

    [Test]
    public void Heal_HasA1_5sCooldown()
    {
        Assert.AreEqual(1.5f, GetCooldown(GetSkill<ApplyConsumerCharacterSkillFactory>("Heal").data), 0.0001f);
    }

    [Test]
    public void Judgment_HealsAllAllies_Plus5PercentPerDevotionSpent()
    {
        JudgmentCharacterSkillData judgment = GetSkill<JudgmentCharacterSkillFactory>("Judgment").data;

        Assert.IsFalse(judgment.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, judgment.entityType);
        Assert.AreEqual(ResourceType.Devotion, judgment.spentResource);
        Assert.AreEqual(0.2f, judgment.multiplier, 0.0001f);
        Assert.AreEqual(0.05f, judgment.multiplierPerSpent, 0.0001f);
        Assert.AreEqual(10f, GetCost(judgment), 0.0001f);
    }

    [Test]
    public void MendingLight_Heals25PercentThenJumps3Times_1DevotionPerHeal()
    {
        MendingLightCharacterSkillData mending = GetSkill<MendingLightCharacterSkillFactory>("Mending Light").data;

        Assert.IsTrue(mending.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, mending.entityType);
        Assert.AreEqual(0.25f, mending.multiplier, 0.0001f);
        Assert.AreEqual(3, mending.jumps);
        Assert.AreEqual(ResourceType.Devotion, mending.gainedResource);
        // A negative consumer value gives the resource
        GameObject character = new GameObject("Character");
        try
        {
            Assert.AreEqual(1f, mending.gainPerHeal.GetConsumer(character, character).GetValue(), 0.0001f, "Not 1 Devotion per heal");
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
        Assert.AreEqual(8f, GetCost(mending), 0.0001f);
        Assert.AreEqual(3f, GetCooldown(mending), 0.0001f);
    }

    // The anticipation spell of the Cleric: cast on the unit about to take a big hit, percent armor only, 15% per
    // Devotion spent up to 75%
    [Test]
    public void Shield_Gives15PercentArmorPerDevotion_UpTo75_ToASingleAllyFor5s()
    {
        ShieldCharacterSkillFactory shield = GetSkill<ShieldCharacterSkillFactory>("Shield");

        Assert.AreEqual(ResourceType.Devotion, shield.data.spentResource);
        Assert.AreEqual(4f, GetCost(shield.data), 0.0001f);
        Assert.IsTrue(shield.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, shield.data.entityType);
        Assert.AreEqual(1, shield.data.buffHandlerFactory.Count);
        ABuffHandlerFactory handler = shield.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(5f, handler.duration, 0.0001f);
        Assert.AreEqual(5, handler.maxStacks);
        Assert.AreEqual(1, handler.buffFactoryList.Count);

        FlatModifierFactory percentArmor = handler.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(percentArmor);
        Assert.AreEqual(AttributeType.PercentArmor, percentArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Add, percentArmor.data.modifierType);
        Assert.AreEqual(0.15f, percentArmor.data.value, 0.0001f);
    }

    [Test]
    public void Shield_TheDescriptionShowsThePercentArmorOnly()
    {
        CharacterSkillData data = GetSkill<ShieldCharacterSkillFactory>("Shield").data;

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
            Assert.IsTrue(GetSkill<ApplyConsumerCharacterSkillFactory>("Heal").data.consumer.GetConsumer(healer, healer).ignoreConsumerPrevention, "Heal");
            Assert.IsTrue(GetSkill<JudgmentCharacterSkillFactory>("Judgment").data.consumer.GetConsumer(healer, healer).ignoreConsumerPrevention, "Judgment");
            Assert.IsTrue(GetSkill<MendingLightCharacterSkillFactory>("Mending Light").data.consumer.GetConsumer(healer, healer).ignoreConsumerPrevention, "Mending Light");
        }
        finally
        {
            Object.DestroyImmediate(healer);
        }
    }

    [Test]
    public void Cleric_StartsWithTheSacredTomeAndTheBlessing()
    {
        ItemFactory sacredTome = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/PlayerItems/HealPowerItem/HealPowerItem.asset");
        ItemFactory blessing = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/PlayerItems/BlessingItem/BlessingItem.asset");

        CollectionAssert.AreEqual(new[] { sacredTome, blessing }, _cleric.items);
    }

    // Every spell of the Cleric blesses the allies it touches (Blessing item): the player reads it on each spell
    [Test]
    public void EveryClericSkill_SaysItBlessesItsTargets()
    {
        foreach (ACharacterSkillFactory skill in _cleric.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            StringAssert.Contains("Blesses", data.description, data.name);
        }
    }

    [Test]
    public void Cleric_HoldsUpTo5Devotion()
    {
        Assert.AreEqual(5f, _cleric.attributes[AttributeType.DevotionMax]);
    }

    [Test]
    public void Heal_Gives1Devotion_AndStillCostsMana()
    {
        CharacterSkillData heal = GetSkill<ApplyConsumerCharacterSkillFactory>("Heal").Create().GetData();

        ResourceValidatorFactory devotion = (ResourceValidatorFactory)heal.validators.Find(v => v is ResourceValidatorFactory r && r.data.resourceType == ResourceType.Devotion);
        Assert.IsNotNull(devotion, "Heal gives no Devotion");
        // A negative cost: a gain
        Assert.AreEqual(-1f, ResourceValidator.GetCost(devotion.data, null), 0.001f);
        Assert.IsFalse(devotion.data.consumer.data.canBeCritical);
        Assert.IsFalse(devotion.data.useCostMultiplier, "The cost items would change the Devotion given");
        Assert.IsTrue(heal.validators.Exists(v => v is ResourceValidatorFactory r && r.data.resourceType == ResourceType.Mana), "Heal costs no mana");
    }

    [Test]
    public void Blessing_EachSpellOnAnAlly_BlessesItFor4s()
    {
        ItemFactory blessing = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/PlayerItems/BlessingItem/BlessingItem.asset");
        Assert.AreEqual("Blessing", blessing.data.name);
        Assert.IsFalse(string.IsNullOrEmpty(blessing.data.description));
        Assert.IsNotNull(blessing.data.icon);

        // Held by the character as long as it has the item
        ABuffHandlerFactory itemHandler = blessing.data.buffs.Find(handler => handler.buffFactoryList.Exists(buff => buff is ApplyBuffOnSkillTargetBuffFactory));
        Assert.IsNotNull(itemHandler, "The Blessing gives nothing when a spell is used");
        Assert.AreEqual(DurationType.Infinite, itemHandler.durationType);
        ApplyBuffOnSkillTargetBuffFactory onSkill = (ApplyBuffOnSkillTargetBuffFactory)itemHandler.buffFactoryList.Find(buff => buff is ApplyBuffOnSkillTargetBuffFactory);
        Assert.AreEqual(Entity.EntityType.Player, onSkill.data.entityType, "Only the allies are blessed");

        // Only the status, nothing more
        Assert.IsNull(onSkill.data.buffHandlerFactory);
        StatusData status = onSkill.data.status;
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<StatusData>(BlessedStatusPath), status);
        Assert.AreEqual("Blessed", status.title);
        Assert.IsFalse(string.IsNullOrEmpty(status.description));
        Assert.AreEqual("Blessed", status.tag.name);
        Assert.IsTrue(status.GivesItsTag(), "The Blessed buff doesn't give the Blessed tag, or isn't tagged with it");

        ABuffHandlerFactory blessed = status.buffHandlerFactory;
        Assert.AreEqual(DurationType.Duration, blessed.durationType);
        Assert.AreEqual(4f, blessed.duration, 0.001f);
        // A new spell refreshes it, whoever blessed the unit: the tag can't go away while another blessing lasts
        Assert.AreEqual(1, blessed.maxStacks);
        Assert.IsTrue(blessed.stackAcrossSources);
        Assert.IsNotNull(blessed.icon, "A blessed unit shows it");
    }

    // A slower start: one damage dealer and one tank, the other units are recruited along the run
    [Test]
    public void Cleric_StartsWithRadiantArcherAndPaladinOnly()
    {
        List<string> titles = _cleric.entities.ConvertAll(entity => entity.title);

        CollectionAssert.AreEqual(new[] { "Radiant Archer", "Paladin" }, titles);
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
    [TestCase("ZealotEntity/ZealotEntity", TagNames.Damage)]
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

    // Recruited along the run only
    [TestCase("BeaconEntity/BeaconEntity")]
    [TestCase("ZealotEntity/ZealotEntity")]
    [TestCase("PurifierEntity/PurifierEntity")]
    public void RecruitOnlyUnit_IsNotAStartingUnit(string path)
    {
        CollectionAssert.DoesNotContain(_cleric.entities, LoadUnit(path));
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

    // The buff given by the effect reacting to the Blessed status
    static ABuffHandlerFactory AssertOnBlessed(ABuffFactory buff, TagTrigger trigger)
    {
        ApplyBuffOnTagBuffFactory onTag = buff as ApplyBuffOnTagBuffFactory;
        Assert.IsNotNull(onTag, "no Blessed effect");
        Assert.AreEqual("Blessed", onTag.data.tag.name);
        Assert.AreEqual(trigger, onTag.data.trigger);
        Assert.IsNotNull(onTag.data.buffHandlerFactory);
        return onTag.data.buffHandlerFactory;
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
    public void RadiantArcher_EachBlessingGivesPlus3DamageAndPlus50PercentAttackSpeedFor5s()
    {
        EntityData archer = LoadUnit("RadiantArcherEntity/RadiantArcherEntity");
        Assert.AreEqual(5f, archer.attributes[AttributeType.Damage]);
        // An attack every 0.9s
        Assert.AreEqual(0.9f, archer.attributes[AttributeType.AttackRate], 0.0001f);
        Assert.AreEqual(1, archer.skillFactories.Count, "it attacks");

        // Every blessing, even on an archer already blessed
        ABuffHandlerFactory radiance = AssertOnBlessed(GetItemBuff<ApplyBuffOnTagBuffFactory>(archer), TagTrigger.EachApplication);

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
    public void Paladin_EachBlessingGivesPlus1ArmorFor10s_UpTo3()
    {
        EntityData paladin = LoadUnit("PaladinEntity/PaladinEntity");
        Assert.AreEqual(200f, paladin.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(1, paladin.skillFactories.Count, "it attacks");

        // Renamed: the Devotion is the resource of the Cleric
        Assert.AreEqual("Faithful Guard", ((ItemFactory)paladin.items[0]).data.name);
        ABuffHandlerFactory devotion = AssertOnBlessed(GetItemBuff<ApplyBuffOnTagBuffFactory>(paladin), TagTrigger.EachApplication);

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

    // Its blessings give the Blessed status, the same as the spells (one status, shown with its icon): the Blessed
    // effects of the units react to them as to the spells
    [Test]
    public void Purifier_EachOfItsBlessingsGivesTheBlessedStatusOfTheSpells()
    {
        PurifySkillFactory purify = (PurifySkillFactory)LoadUnit("PurifierEntity/PurifierEntity").skillFactories[0];
        ApplyBuffOnSkillTargetBuffFactory onSkill = AssetDatabase.LoadAssetAtPath<ApplyBuffOnSkillTargetBuffFactory>("Assets/Data/PlayerItems/BlessingItem/ApplyBuffOnSkillTargetBuffFactory.asset");

        Assert.IsNotNull(purify.data.status);
        Assert.AreSame(onSkill.data.status, purify.data.status);
        // The tag comes from the status only, never hidden in a buff with another icon
        foreach (ABuffHandlerFactory buff in purify.data.buffHandlerFactories)
        {
            Assert.IsFalse(buff.buffFactoryList.Exists(b => b is AddTagBuffFactory), buff.name);
            Assert.IsFalse(buff.HasTag("Blessed"), buff.name);
        }
    }

    // Swift Grace: each blessing, even on an already blessed Purifier, brings its next use 1s closer
    [Test]
    public void Purifier_EachBlessing_ItsNextUseComes1sSooner()
    {
        EntityData purifier = LoadUnit("PurifierEntity/PurifierEntity");
        ItemFactory swiftGrace = (ItemFactory)purifier.items.Find(item => item != null && item.name == "SwiftGraceItem");
        Assert.IsNotNull(swiftGrace, "The Purifier has no Swift Grace");
        Assert.AreEqual("Swift Grace", swiftGrace.data.name);
        Assert.IsFalse(string.IsNullOrEmpty(swiftGrace.data.description));
        Assert.IsNotNull(swiftGrace.data.icon);

        ABuffHandlerFactory held = swiftGrace.data.buffs.Find(handler => handler.buffFactoryList.Exists(buff => buff is ApplyBuffOnTagBuffFactory));
        Assert.IsNotNull(held);
        Assert.AreEqual(DurationType.Infinite, held.durationType);
        ApplyBuffOnTagBuffFactory onBlessed = (ApplyBuffOnTagBuffFactory)held.buffFactoryList.Find(buff => buff is ApplyBuffOnTagBuffFactory);
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<StatusData>(BlessedStatusPath).tag, onBlessed.data.tag);
        Assert.AreEqual(TagTrigger.EachApplication, onBlessed.data.trigger, "Only the first blessing would count");

        ABuffHandlerFactory reduction = onBlessed.data.buffHandlerFactory;
        Assert.AreEqual(DurationType.Instant, reduction.durationType);
        ReduceCooldownBuffFactory reduce = (ReduceCooldownBuffFactory)reduction.buffFactoryList.Find(buff => buff is ReduceCooldownBuffFactory);
        Assert.IsNotNull(reduce);
        Assert.AreEqual(1f, reduce.data.seconds, 0.0001f);
    }

    // In play: blessed twice in a row, the second while still blessed, its cooldown loses 2s
    [Test]
    public void Purifier_BlessedTwice_ItsCooldownLoses2s()
    {
        EntityData purifier = LoadUnit("PurifierEntity/PurifierEntity");
        ItemFactory swiftGrace = (ItemFactory)purifier.items.Find(item => item != null && item.name == "SwiftGraceItem");
        StatusData blessed = AssetDatabase.LoadAssetAtPath<StatusData>(BlessedStatusPath);
        Entities.TestUnits units = new Entities.TestUnits();
        try
        {
            Entity unit = units.Create(100f, 100f, "Purifier");
            BuffManager buffManager = unit.GetComponent<BuffManager>();
            buffManager.isEnabled = true;
            foreach (ABuffHandlerFactory handler in swiftGrace.data.buffs)
            {
                buffManager.AddHandler(handler, unit.gameObject, unit.gameObject);
            }
            buffManager.ForceUpdate();
            // Just used: 4s before the next use
            Skills.FakeCooldownSkill skill = unit.gameObject.AddComponent<Skills.FakeCooldownSkill>();
            skill.data = new SkillDataBase { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };
            skill.Tick(unit.gameObject, 0f);

            unit.ApplyStatus(blessed, unit.gameObject);
            buffManager.ForceUpdate();
            Assert.AreEqual(3f, skill.timeBeforeUse, 0.0001f, "first blessing");

            unit.ApplyStatus(blessed, unit.gameObject);
            buffManager.ForceUpdate();
            Assert.IsTrue(unit.HasStatus(blessed));
            Assert.AreEqual(2f, skill.timeBeforeUse, 0.0001f, "blessed again while blessed");
        }
        finally
        {
            units.DestroyAll();
        }
    }

    // A Cleric unit item: +2 armor as long as the unit is blessed
    [Test]
    public void HolyWater_2ArmorWhileBlessed_OnlyForTheCleric()
    {
        ItemFactory holyWater = AssetDatabase.LoadAssetAtPath<ItemFactory>("Assets/Data/EntityItems/HolyWaterItem/HolyWaterItem.asset");
        Assert.IsNotNull(holyWater);
        Assert.AreEqual("Holy Water", holyWater.data.name);
        Assert.IsFalse(string.IsNullOrEmpty(holyWater.data.description));
        Assert.IsNotNull(holyWater.data.icon);
        Assert.IsTrue(holyWater.HasTag(TagNames.Entity));
        Assert.IsTrue(holyWater.HasTag(_cleric.classTag), "Offered to the other classes, where it does nothing");
        Assert.IsTrue(AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset").items.Contains(holyWater), "Never dropped");

        ABuffHandlerFactory held = holyWater.data.buffs.Find(handler => handler.buffFactoryList.Exists(buff => buff is ApplyBuffOnTagBuffFactory));
        Assert.IsNotNull(held);
        Assert.AreEqual(DurationType.Infinite, held.durationType);
        ApplyBuffOnTagBuffFactory whileBlessed = (ApplyBuffOnTagBuffFactory)held.buffFactoryList.Find(buff => buff is ApplyBuffOnTagBuffFactory);
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<StatusData>(BlessedStatusPath).tag, whileBlessed.data.tag);
        Assert.AreEqual(TagTrigger.WhileTagged, whileBlessed.data.trigger);
        ABuffHandlerFactory armor = whileBlessed.data.buffHandlerFactory;
        Assert.AreEqual(DurationType.Infinite, armor.durationType, "taken back with the tag");
        Assert.AreEqual(1, armor.buffFactoryList.Count);
        AssertModifier(armor.buffFactoryList[0], AttributeType.FlatArmor, AttributeModifierType.Add, 2f);
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
        // The allies it heals are shown around it, in green like the heals
        ShowRelativeCellsBuffFactory zone = (ShowRelativeCellsBuffFactory)((ItemFactory)beacon.items[0]).data.buffs[0].buffFactoryList.Find(buff => buff is ShowRelativeCellsBuffFactory);
        Assert.IsNotNull(zone, "the zone is not shown");
        Assert.AreEqual(share.data.pattern, zone.data.pattern);
        Assert.AreEqual(share.data.range, zone.data.range);
        Assert.AreEqual("HealCell", zone.data.cellPrefab.name);
    }

    [Test]
    public void Beacon_Copies60PercentWhileBlessed()
    {
        ShareCharacterHealBuffFactory share = GetItemBuff<ShareCharacterHealBuffFactory>(LoadUnit("BeaconEntity/BeaconEntity"));

        Assert.IsNotNull(share.data.boostTag);
        Assert.AreEqual("Blessed", share.data.boostTag.name);
        Assert.AreEqual(0.6f, share.data.boostedRatio, 0.0001f);
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
    public void Zealot_Deals50PercentMoreDamageAbove70PercentHealth()
    {
        EntityData zealot = LoadUnit("ZealotEntity/ZealotEntity");
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

    [Test]
    public void Zealot_Gets30PercentAttackSpeedWhileBlessed()
    {
        ItemFactory zeal = (ItemFactory)LoadUnit("ZealotEntity/ZealotEntity").items[0];
        ABuffFactory onBlessed = zeal.data.buffs[0].buffFactoryList.Find(buff => buff is ApplyBuffOnTagBuffFactory);

        ABuffHandlerFactory fervor = AssertOnBlessed(onBlessed, TagTrigger.WhileTagged);

        Assert.AreEqual(DurationType.Infinite, fervor.durationType, "lasts as long as the blessing");
        Assert.AreEqual(1, fervor.buffFactoryList.Count);
        // The attack cooldown x0.77: 1.3 times as many attacks
        AssertModifier(fervor.buffFactoryList[0], AttributeType.AttackRate, AttributeModifierType.Multiply, -0.23f);
    }

    // The units react to the blessings rather than the heals: their descriptions say it
    [TestCase("RadiantArcherEntity/RadiantArcherEntity")]
    [TestCase("PaladinEntity/PaladinEntity")]
    [TestCase("ZealotEntity/ZealotEntity")]
    [TestCase("PurifierEntity/PurifierEntity")]
    [TestCase("BeaconEntity/BeaconEntity")]
    public void ClericUnit_DescriptionMentionsTheBlessing(string path)
    {
        EntityData unit = LoadUnit(path);

        StringAssert.Contains("bless", unit.description.ToLowerInvariant(), unit.title);
    }
}

}
