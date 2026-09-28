using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Warlock data: Drain Life, Raise Dead, Curse and Dark Pact, and the Blood Cultist unit
public class WarlockDataTests
{
    CharacterData _warlock;
    GameObject _caster;
    Character _character;

    [SetUp]
    public void SetUp()
    {
        _warlock = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/WarlockCharacter/WarlockCharacter.asset");
        Assert.IsNotNull(_warlock);
        _caster = new GameObject("Warlock");
        AttributeManager attributes = TestHelpers.CreateAttributeManager(_caster, AttributeType.HealPower, 15f);
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its attributes are read
        TestHelpers.WithLoggingDisabled(() => _character = _caster.AddComponent<Character>());
        _character.attributeManager = attributes;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_caster);
    }

    T GetSkill<T>(string name) where T : ACharacterSkillFactory
    {
        ACharacterSkillFactory skill = _warlock.skills.Find(s => s != null && s.Create().GetData().name == name);
        Assert.IsNotNull(skill, "The Warlock has no " + name + " skill");
        Assert.IsInstanceOf<T>(skill, name);
        return (T)skill;
    }

    string GetDescription(string name)
    {
        CharacterSkillData data = _warlock.skills.Find(s => s.Create().GetData().name == name).Create().GetData();
        return TextConvertor.Convert(data.description, _character, data);
    }

    [Test]
    public void Warlock_HasDrainLifeRaiseDeadCurseAndDarkPact()
    {
        List<string> names = _warlock.skills.ConvertAll(skill => skill.Create().GetData().name);

        CollectionAssert.AreEqual(new[] { "Drain Life", "Raise Dead", "Curse", "Dark Pact" }, names);
    }

    [Test]
    public void EveryWarlockSkill_HasAnIconADescriptionAndACooldown()
    {
        foreach (ACharacterSkillFactory skill in _warlock.skills)
        {
            CharacterSkillData data = skill.Create().GetData();
            Assert.IsNotNull(data.icon, data.name);
            Assert.IsFalse(string.IsNullOrEmpty(data.description), data.name);
            Assert.IsTrue(data.validators.Exists(v => v is DurationValidatorFactory), data.name + " has no cooldown");
        }
    }

    [Test]
    public void DrainLife_DamagesAnEnemyWithTheHealPower_AndHealsAllies()
    {
        DrainLifeCharacterSkillFactory drain = GetSkill<DrainLifeCharacterSkillFactory>("Drain Life");

        Assert.IsTrue(drain.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Computer, drain.data.entityType);
        Assert.AreEqual(Entity.EntityType.Player, drain.data.allyType);
        Assert.AreEqual(1f, drain.data.healRatio, 0.0001f);
        AConsumer damage = drain.data.consumer.GetConsumer(_caster, _caster);
        // Damage is negative, reduced by the armor and blocked by the invincibility like any hit
        Assert.AreEqual(-15f, damage.GetValue() * drain.data.multiplier, 0.0001f);
        Assert.IsFalse(damage.ignoreDamageReduction);
        Assert.IsFalse(damage.ignoreConsumerPrevention);
        Assert.IsTrue(drain.data.validators.Exists(v => v is ResourceValidatorFactory));
    }

    [Test]
    public void RaiseDead_SummonsARisenSkeleton_TwoAtMost()
    {
        RaiseDeadCharacterSkillFactory raise = GetSkill<RaiseDeadCharacterSkillFactory>("Raise Dead");

        Assert.IsNotNull(raise.data.entity);
        Assert.AreEqual("Risen Skeleton", raise.data.entity.title);
        Assert.AreEqual(2, raise.data.maxAlive);
        Assert.AreEqual(3f, raise.data.healthPerHealPower, 0.0001f);
        Assert.AreEqual(0.1f, raise.data.damagePerHealPower, 0.0001f);
        Assert.IsTrue(raise.data.validators.Exists(v => v is ResourceValidatorFactory));
    }

    [Test]
    public void Curse_PoisonsAnEnemyIgnoringItsArmor()
    {
        BuffCharacterSkillFactory curse = GetSkill<BuffCharacterSkillFactory>("Curse");

        Assert.IsTrue(curse.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Computer, curse.data.entityType);
        BuffHandlerFactory handler = (BuffHandlerFactory)curse.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.data.durationType);
        Assert.IsTrue(handler.data.isPeriodic);
        Assert.AreEqual(1, handler.data.maxStacks);
        AConsumer damage = ((ApplyConsumerBuffFactory)handler.data.buffFactoryList[0]).data.consumerFactory.GetConsumer(_caster, _caster);
        // 30% of the 15 Heal Power per tick
        Assert.AreEqual(-4.5f, damage.GetValue(), 0.0001f);
        Assert.IsTrue(damage.ignoreDamageReduction);
        Assert.IsFalse(damage.ignoreConsumerPrevention);
    }

    [Test]
    public void DarkPact_Sacrifices15PercentHealthForManaWithoutManaCost()
    {
        DarkPactCharacterSkillFactory pact = GetSkill<DarkPactCharacterSkillFactory>("Dark Pact");

        Assert.AreEqual(0.15f, pact.data.healthPercent, 0.0001f);
        Assert.Greater(pact.data.manaPerHealth, 0f);
        Assert.AreEqual(Entity.EntityType.Player, pact.data.entityType);
        Assert.IsFalse(pact.data.validators.Exists(v => v is ResourceValidatorFactory));
    }

    // The descriptions read their values from the data and the Heal Power of the character
    [TestCase("Drain Life", "<color=\"red\">15</color> <color=#008080ff>(100% HealPower)</color>")]
    [TestCase("Raise Dead", "<color=\"green\">+45</color> health and <color=\"red\">+1.5</color> damage")]
    [TestCase("Curse", "<color=\"red\">4.5</color> <color=#008080ff>(30% HealPower)</color>")]
    [TestCase("Dark Pact", "<color=\"red\">15%</color> of its max health")]
    public void Description_ShowsTheValuesOfTheData(string name, string expected)
    {
        StringAssert.Contains(expected, GetDescription(name));
    }

    [Test]
    public void BloodCultist_HealsItsMostWoundedAllyForHalfItsDamage()
    {
        EntityData cultist = _warlock.entities.Find(entity => entity != null && entity.title == "Blood Cultist");
        Assert.IsNotNull(cultist, "The Warlock can't recruit the Blood Cultist");

        ItemFactory bond = cultist.items[0] as ItemFactory;
        Assert.IsNotNull(bond);
        BuffHandlerFactory handler = (BuffHandlerFactory)bond.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.data.durationType);
        LifeStealBuffFactory lifeSteal = handler.data.buffFactoryList[0] as LifeStealBuffFactory;
        Assert.IsNotNull(lifeSteal);
        Assert.AreEqual(0.5f, lifeSteal.data.ratio, 0.0001f);
    }
}

}
