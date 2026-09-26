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
    public void FormatItem_WithoutDescription_HasNoDash()
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { name = "Venom" };

        StringAssert.DoesNotContain("—", EntityInfoFormatter.FormatItem(itemFactory.GetItem(), true));
    }

    #endregion
}

}
