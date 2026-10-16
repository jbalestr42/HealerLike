using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

public class EntityInfoFormatterTests
{
    class FixedModifier : AttributeModifier
    {
        public float value;
        public override float ApplyModifier() => value;
    }

    GameObject _owner;
    GameObject _source;
    readonly List<Object> _objects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Owner");
        _source = new GameObject("Source");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_source);
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    BuffHandlerFactory CreateHandlerFactory(string name, List<ABuffFactory> buffs, DurationType durationType = DurationType.Duration, float duration = 10f)
    {
        BuffHandlerFactory handlerFactory = CreateTracked<BuffHandlerFactory>();
        handlerFactory.name = name;
        handlerFactory.data = new BuffHandlerData
        {
            durationType = durationType,
            duration = duration,
            buffFactoryList = buffs,
            tags = new List<GameplayTag>(),
        };
        return handlerFactory;
    }

    FlatModifierFactory CreateFlatModifier(AttributeType type)
    {
        FlatModifierFactory modifierFactory = CreateTracked<FlatModifierFactory>();
        modifierFactory.data = new FlatModifierData { type = type, modifierType = AttributeModifierType.Add, value = 1f };
        return modifierFactory;
    }

    static Attribute.SourceModifier CreateSourceModifier(GameObject source, float value)
    {
        return new Attribute.SourceModifier { source = source, modifier = new FixedModifier { value = value } };
    }

    #region Names

    [Test]
    public void Prettify_RemovesTheSuffixesAndSplitsTheWords()
    {
        Assert.AreEqual("Shoot Projectile", EntityInfoFormatter.Prettify("ShootProjectileSkill", "Skill"));
        Assert.AreEqual("Apply Consumer", EntityInfoFormatter.Prettify("ApplyConsumerBuffFactory", "Factory", "Buff"));
        Assert.AreEqual("Regen Hp Item", EntityInfoFormatter.Prettify("RegenHp_Item"));
    }

    [Test]
    public void Prettify_KeepsANameMadeOnlyOfTheSuffix()
    {
        Assert.AreEqual("Skill", EntityInfoFormatter.Prettify("Skill", "Skill"));
    }

    [Test]
    public void GroupDuplicates_MergesIdenticalLinesKeepingTheirOrder()
    {
        List<string> lines = new List<string> { "Poison", "Slow", "Poison", "Poison" };

        CollectionAssert.AreEqual(new[] { "Poison ×3", "Slow" }, EntityInfoFormatter.GroupDuplicates(lines));
    }

    [Test]
    public void GroupDuplicates_DistinctLines_AreUnchanged()
    {
        CollectionAssert.AreEqual(new[] { "A", "B" }, EntityInfoFormatter.GroupDuplicates(new List<string> { "A", "B" }));
    }

    [Test]
    public void GetAttributeName_IsTranslated()
    {
        Assert.AreEqual("Damage", EntityInfoFormatter.GetAttributeName(AttributeType.Damage));
        Assert.AreEqual("Vulnerability", EntityInfoFormatter.GetAttributeName(AttributeType.Vulnerability));
        Assert.AreEqual("Healing Received", EntityInfoFormatter.GetAttributeName(AttributeType.HealingReceived));
    }

    [Test]
    public void GetSourceName_MissingSource_SaysItIsGone()
    {
        Assert.AreEqual("missing source", EntityInfoFormatter.GetSourceName(null, _owner));
    }

    [Test]
    public void GetSourceName_Owner_IsItself()
    {
        Assert.AreEqual("itself", EntityInfoFormatter.GetSourceName(_owner, _owner));
    }

    [Test]
    public void GetSourceName_OtherObject_IsItsName()
    {
        Assert.AreEqual("Source", EntityInfoFormatter.GetSourceName(_source, _owner));
    }

    #endregion

    #region Attributes

    [Test]
    public void FormatAttribute_WithoutModifier_DoesNotShowTheBase()
    {
        string line = EntityInfoFormatter.FormatAttribute(AttributeType.Damage, new Attribute(10f));

        Assert.AreEqual("Damage: <b>10</b>", line);
    }

    [Test]
    public void FormatAttribute_Increased_ShowsTheBaseAsABonus()
    {
        Attribute attribute = new Attribute(10f);
        attribute.AddModifier(AttributeModifierType.Add, _source, new FixedModifier { value = 2.5f });
        attribute.Update();

        string line = EntityInfoFormatter.FormatAttribute(AttributeType.Damage, attribute);

        StringAssert.StartsWith("Damage: <b>12.5</b>", line);
        StringAssert.Contains($"<color={EntityInfoFormatter.BonusColor}>(base 10)", line);
    }

    [Test]
    public void FormatAttribute_Decreased_ShowsTheBaseAsAMalus()
    {
        Attribute attribute = new Attribute(10f);
        attribute.AddModifier(AttributeModifierType.Add, _source, new FixedModifier { value = -4f });
        attribute.Update();

        StringAssert.Contains($"<color={EntityInfoFormatter.MalusColor}>(base 10)", EntityInfoFormatter.FormatAttribute(AttributeType.Damage, attribute));
    }

    [Test]
    public void FormatModifier_Add_ShowsTheSignedValueAndTheSource()
    {
        StringAssert.Contains("+2 · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Add, CreateSourceModifier(_source, 2f), _owner));
        StringAssert.Contains("-1 · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Add, CreateSourceModifier(_source, -1f), _owner));
    }

    [Test]
    public void FormatModifier_Multiply_IsAPercentage()
    {
        StringAssert.Contains("+30 % · itself", EntityInfoFormatter.FormatModifier(AttributeModifierType.Multiply, CreateSourceModifier(_owner, 0.3f), _owner));
    }

    [Test]
    public void FormatModifier_Override_ShowsTheForcedValue()
    {
        StringAssert.Contains("= 5 · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Override, CreateSourceModifier(_source, 5f), _owner));
    }

    // The attack cooldown is shown as a speed, attacks per second: what the "+50% attack speed" of the descriptions read
    [Test]
    public void FormatAttribute_AttackCooldown_IsShownAsAttacksPerSecond()
    {
        Assert.AreEqual("Attack Speed: <b>2/s</b>", EntityInfoFormatter.FormatAttribute(AttributeType.AttackRate, new Attribute(0.5f)));
    }

    [Test]
    public void FormatAttribute_ShorterAttackCooldown_IsAFasterSpeed_ShownAsABonus()
    {
        Attribute attribute = new Attribute(1f);
        attribute.AddModifier(AttributeModifierType.Multiply, _source, new FixedModifier { value = 1f / 1.5f - 1f });
        attribute.Update();

        string line = EntityInfoFormatter.FormatAttribute(AttributeType.AttackRate, attribute);

        StringAssert.StartsWith("Attack Speed: <b>1.5/s</b>", line);
        StringAssert.Contains($"<color={EntityInfoFormatter.BonusColor}>(base 1/s)", line);
    }

    [Test]
    public void FormatAttribute_LongerAttackCooldown_IsASlowerSpeed_ShownAsAMalus()
    {
        Attribute attribute = new Attribute(1f);
        attribute.AddModifier(AttributeModifierType.Add, _source, new FixedModifier { value = 1f });
        attribute.Update();

        string line = EntityInfoFormatter.FormatAttribute(AttributeType.AttackRate, attribute);

        StringAssert.StartsWith("Attack Speed: <b>0.5/s</b>", line);
        StringAssert.Contains($"<color={EntityInfoFormatter.MalusColor}>(base 1/s)", line);
    }

    [Test]
    public void FormatModifier_AttackCooldownMultiplier_IsTheAttackSpeedItGives()
    {
        // A cooldown divided by 1.5 is 50% more attacks, a doubled one half as many
        StringAssert.Contains("+50 % · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Multiply, CreateSourceModifier(_source, 1f / 1.5f - 1f), _owner, AttributeType.AttackRate));
        StringAssert.Contains("-50 % · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Multiply, CreateSourceModifier(_source, 1f), _owner, AttributeType.AttackRate));
    }

    [Test]
    public void FormatModifier_AttackCooldownAddOrOverride_IsInSecondsOfCooldown()
    {
        StringAssert.Contains("+0.5s cooldown · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Add, CreateSourceModifier(_source, 0.5f), _owner, AttributeType.AttackRate));
        StringAssert.Contains("= 2s cooldown · Source", EntityInfoFormatter.FormatModifier(AttributeModifierType.Override, CreateSourceModifier(_source, 2f), _owner, AttributeType.AttackRate));
    }

    [Test]
    public void GetAttributeLines_ShowsTheAttackCooldownModifiersAsAttackSpeed()
    {
        AttributeManager attributeManager = TestHelpers.CreateAttributeManager(_owner, AttributeType.AttackRate, 1f);
        attributeManager.Get(AttributeType.AttackRate).AddModifier(AttributeModifierType.Multiply, _source, new FixedModifier { value = 1f / 1.5f - 1f });
        attributeManager.Get(AttributeType.AttackRate).Update();

        List<string> lines = EntityInfoFormatter.GetAttributeLines(attributeManager, _owner);

        Assert.AreEqual(2, lines.Count);
        StringAssert.StartsWith("Attack Speed: <b>1.5/s</b>", lines[0]);
        StringAssert.Contains("+50 % · Source", lines[1]);
    }

    [Test]
    public void GetAttributeLines_ListsEachAttributeFollowedByItsModifiers()
    {
        AttributeManager attributeManager = TestHelpers.CreateAttributeManager(_owner, AttributeType.Damage, 10f);
        attributeManager.Add(AttributeType.HealthMax, new Attribute(100f));
        attributeManager.Get(AttributeType.Damage).AddModifier(AttributeModifierType.Add, _source, new FixedModifier { value = 2f });
        attributeManager.Get(AttributeType.Damage).Update();

        List<string> lines = EntityInfoFormatter.GetAttributeLines(attributeManager, _owner);

        Assert.AreEqual(3, lines.Count);
        StringAssert.StartsWith("Max HP", lines[0]);
        StringAssert.StartsWith("Damage", lines[1]);
        StringAssert.Contains("+2 · Source", lines[2]);
    }

    #endregion

    #region Buffs

    [Test]
    public void GetBuffName_MeaningfulAssetName_IsKept()
    {
        Assert.AreEqual("Venom", EntityInfoFormatter.GetBuffName(CreateHandlerFactory("Venom", new List<ABuffFactory>())));
    }

    [Test]
    public void GetBuffName_PrefixedGenericName_KeepsThePrefix()
    {
        BuffHandlerFactory handlerFactory = CreateHandlerFactory("PoisonSingleTarget_BuffHandlerFactory", new List<ABuffFactory>());

        Assert.AreEqual("Poison Single Target", EntityInfoFormatter.GetBuffName(handlerFactory));
    }

    [TestCase("BuffHandlerFactory")]
    [TestCase("BuffHandlerFactory 1")]
    [TestCase("New Buff Handler Factory")]
    public void GetBuffName_GenericName_DescribesTheBuffs(string name)
    {
        BuffHandlerFactory handlerFactory = CreateHandlerFactory(name, new List<ABuffFactory> { CreateFlatModifier(AttributeType.Vulnerability) });

        Assert.AreEqual("Vulnerability (Flat Modifier)", EntityInfoFormatter.GetBuffName(handlerFactory));
    }

    [Test]
    public void GetBuffName_GenericNameWithoutBuff_IsAnEffect()
    {
        Assert.AreEqual("Effect", EntityInfoFormatter.GetBuffName(CreateHandlerFactory("BuffHandlerFactory", new List<ABuffFactory>())));
    }

    [Test]
    public void DescribeBuff_NotAnAttributeModifier_IsItsType()
    {
        Buff.FakeBuffFactory buffFactory = CreateTracked<Buff.FakeBuffFactory>();
        buffFactory.data = new Buff.FakeBuffData();

        Assert.AreEqual("Fake", EntityInfoFormatter.DescribeBuff(buffFactory));
    }

    [Test]
    public void FormatBuff_ShowsTheStacksTheRemainingTimeAndTheSource()
    {
        BuffManager.BuffHandlerData buffHandlerData = new BuffManager.BuffHandlerData
        {
            buffHandlerFactory = CreateHandlerFactory("Venom", new List<ABuffFactory>(), DurationType.Duration, 5f),
            source = _source,
            target = _owner,
            currentStacks = 2,
        };
        buffHandlerData.buffHandler = buffHandlerData.buffHandlerFactory.GetBuffHandler();
        buffHandlerData.buffHandler.Start(_source, _owner);
        buffHandlerData.buffHandler.Update(1.5f);

        string line = EntityInfoFormatter.FormatBuff(buffHandlerData, _owner);

        StringAssert.StartsWith("<b>Venom</b> ×2", line);
        StringAssert.Contains("· 3.5s · from Source", line);
    }

    [Test]
    public void FormatBuff_Infinite_IsPermanent()
    {
        BuffManager.BuffHandlerData buffHandlerData = new BuffManager.BuffHandlerData
        {
            buffHandlerFactory = CreateHandlerFactory("Venom", new List<ABuffFactory>(), DurationType.Infinite),
            source = _owner,
            target = _owner,
            currentStacks = 1,
        };
        buffHandlerData.buffHandler = buffHandlerData.buffHandlerFactory.GetBuffHandler();

        string line = EntityInfoFormatter.FormatBuff(buffHandlerData, _owner);

        StringAssert.DoesNotContain("×", line);
        StringAssert.Contains("· permanent · from itself", line);
    }

    [Test]
    public void GetBuffLines_OneLinePerActiveHandler()
    {
        Buff.FakeBuffFactory buffFactory = CreateTracked<Buff.FakeBuffFactory>();
        buffFactory.data = new Buff.FakeBuffData();
        BuffManager buffManager = _owner.AddComponent<BuffManager>();
        buffManager.isEnabled = true;
        buffManager.AddHandler(CreateHandlerFactory("Venom", new List<ABuffFactory> { buffFactory }), _source, _owner);
        buffManager.AddHandler(CreateHandlerFactory("Burn", new List<ABuffFactory> { buffFactory }), _source, _owner);
        buffManager.ForceUpdate();

        List<string> lines = EntityInfoFormatter.GetBuffLines(buffManager, _owner);

        Assert.AreEqual(2, lines.Count);
    }

    #endregion

    #region Skills and items

    [Test]
    public void FormatSkill_Ready_ShowsItsCooldown()
    {
        Skills.FakeCooldownSkill skill = _owner.AddComponent<Skills.FakeCooldownSkill>();
        skill.data = new SkillDataBase { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };

        string line = EntityInfoFormatter.FormatSkill(skill);

        Assert.AreEqual($"<b>Fake Cooldown</b> <color={EntityInfoFormatter.MutedColor}>· cooldown 4.0s · ready</color>", line);
    }

    [Test]
    public void FormatSkill_JustUsed_ShowsTheTimeLeft()
    {
        Skills.FakeCooldownSkill skill = _owner.AddComponent<Skills.FakeCooldownSkill>();
        skill.data = new SkillDataBase { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };
        skill.UpdateBehaviour(_owner);

        StringAssert.Contains("ready in 4.0s", EntityInfoFormatter.FormatSkill(skill));
    }

    [Test]
    public void FormatItem_ShowsTheTitleTheDescriptionAndWhereItComesFrom()
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { name = "Venom", description = "Poisons the target" };
        AItem item = itemFactory.GetItem();

        Assert.AreEqual($"<b>Venom</b> — Poisons the target <color={EntityInfoFormatter.MutedColor}>(innate)</color>", EntityInfoFormatter.FormatItem(item, true));
        StringAssert.EndsWith("(added)</color>", EntityInfoFormatter.FormatItem(item, false));
    }

    [Test]
    public void FormatItemDetails_ShowsTheTitleAndTheDescription()
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { name = "Venom", description = "Poisons the target" };

        Assert.AreEqual("<b>Venom</b> — Poisons the target", EntityInfoFormatter.FormatItemDetails(itemFactory.GetItem()));
    }

    [Test]
    public void FormatItemDetails_WithoutDescription_ShowsTheTitle()
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { name = "Venom" };

        Assert.AreEqual("<b>Venom</b>", EntityInfoFormatter.FormatItemDetails(itemFactory.GetItem()));
    }

    [Test]
    public void FormatItem_WithoutDescription_HasNoDash()
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { name = "Venom" };

        StringAssert.DoesNotContain("—", EntityInfoFormatter.FormatItem(itemFactory.GetItem(), true));
    }

    #endregion

    #region Combat meter

    [Test]
    public void GetCombatMeterLines_InAFight_ShowsTheDpsTheLastSecondsAndTheTotal()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 100f);
        meter.AddDamage(19f, 50f);

        List<string> lines = EntityInfoFormatter.GetCombatMeterLines(meter, 20f);

        Assert.AreEqual(1, lines.Count);
        StringAssert.StartsWith("Fight: <b>7.5</b> DPS", lines[0]);
        StringAssert.Contains("(last 5s: 10)", lines[0]);
        StringAssert.Contains("150 damage in 20.0s", lines[0]);
    }

    [Test]
    public void GetCombatMeterLines_BetweenFights_ShowsTheLastFightWithoutTheLastSeconds()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 100f);
        meter.EndFight(10f);

        List<string> lines = EntityInfoFormatter.GetCombatMeterLines(meter, 50f);

        Assert.AreEqual(1, lines.Count);
        StringAssert.StartsWith("Last fight: <b>10.0</b> DPS", lines[0]);
        StringAssert.DoesNotContain("last 5s", lines[0]);
    }

    [Test]
    public void GetCombatMeterLines_Healer_ShowsOnlyItsHps()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddHeal(1f, 40f);

        List<string> lines = EntityInfoFormatter.GetCombatMeterLines(meter, 10f);

        Assert.AreEqual(1, lines.Count);
        StringAssert.StartsWith("Fight: <b>4.0</b> HPS", lines[0]);
        StringAssert.Contains("40 heal in 10.0s", lines[0]);
    }

    [Test]
    public void GetCombatMeterLines_AfterSeveralFights_AlsoShowsTheRun()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 100f);
        meter.AddHeal(1f, 20f);
        meter.EndFight(10f);
        meter.StartFight(100f);
        meter.AddDamage(101f, 200f);

        List<string> lines = EntityInfoFormatter.GetCombatMeterLines(meter, 110f);

        Assert.AreEqual(4, lines.Count);
        StringAssert.StartsWith("Fight: <b>20.0</b> DPS", lines[0]);
        StringAssert.StartsWith("Fight: <b>0.0</b> HPS", lines[1]);
        StringAssert.StartsWith("Run: <b>15.0</b> DPS", lines[2]);
        StringAssert.Contains("300 damage in 20.0s, 2 fights", lines[2]);
        StringAssert.StartsWith("Run: <b>1.0</b> HPS", lines[3]);
    }

    [Test]
    public void GetCombatMeterLines_FirstFight_HasNoRunLine()
    {
        CombatMeter meter = new CombatMeter();
        meter.StartFight(0f);
        meter.AddDamage(1f, 100f);

        List<string> lines = EntityInfoFormatter.GetCombatMeterLines(meter, 10f);

        Assert.IsFalse(lines.Exists(line => line.StartsWith("Run")));
    }

    #endregion

    ResourceAttribute CreateShield(float value)
    {
        GameObject shieldGo = new GameObject("Shield");
        _objects.Add(shieldGo);
        ResourceAttribute shield = TestHelpers.CreateResourceAttribute(shieldGo, ResourceType.Shield, AttributeType.HealthMax, 100f);
        shield.SetValue(value);
        return shield;
    }

    [Test]
    public void FormatShield_ItsValue()
    {
        Assert.AreEqual("Shield: <b>12</b>", EntityInfoFormatter.FormatShield(CreateShield(12f)));
    }

    // Most units have no shield: no line
    [Test]
    public void FormatShield_Empty_NoLine()
    {
        Assert.IsNull(EntityInfoFormatter.FormatShield(CreateShield(0f)));
        Assert.IsNull(EntityInfoFormatter.FormatShield(null));
    }
}

}
