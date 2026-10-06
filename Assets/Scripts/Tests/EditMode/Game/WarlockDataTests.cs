using System.Collections.Generic;
using System.Linq;
using Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the Warlock data: Drain Life, Curse, Dark Pact and Soul Link, its starting units, and the Blood
// Cultist, Pact Bearer and Hex Weaver units
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
    public void Warlock_HasDrainLifeCurseDarkPactAndSoulLink()
    {
        List<string> names = _warlock.skills.ConvertAll(skill => skill.Create().GetData().name);

        CollectionAssert.AreEqual(new[] { "Drain Life", "Curse", "Dark Pact", "Soul Link" }, names);
    }

    // The anticipation spell of the Warlock: cast on the unit about to take a big hit
    [Test]
    public void SoulLink_SplitsTheDamageOfASingleAllyWithTheTeamFor5s()
    {
        BuffCharacterSkillFactory link = GetSkill<BuffCharacterSkillFactory>("Soul Link");

        Assert.IsTrue(link.data.isSingle);
        Assert.AreEqual(Entity.EntityType.Player, link.data.entityType);
        Assert.AreEqual(1, link.data.buffHandlerFactory.Count);
        ABuffHandlerFactory handler = link.data.buffHandlerFactory[0];
        Assert.AreEqual(DurationType.Duration, handler.durationType);
        Assert.AreEqual(5f, handler.duration, 0.0001f);
        Assert.AreEqual(1, handler.buffFactoryList.Count);
        Assert.IsInstanceOf<SoulLinkBuffFactory>(handler.buffFactoryList[0]);
        Assert.IsTrue(link.data.validators.Exists(v => v is ResourceValidatorFactory), "Soul Link has no mana cost");
    }

    [Test]
    public void SoulLink_TheDescriptionShowsTheDuration()
    {
        CharacterSkillData data = GetSkill<BuffCharacterSkillFactory>("Soul Link").data;

        string description = TextConvertor.Convert(data.description, _character, data);

        StringAssert.Contains("during <color=\"green\">5s</color>", description);
        StringAssert.Contains("split evenly", description);
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

    EntityData GetUnit(string title)
    {
        EntityData unit = _warlock.entities.Find(entity => entity != null && entity.title == title);
        Assert.IsNotNull(unit, "The Warlock doesn't start with the " + title);
        return unit;
    }

    [Test]
    public void Warlock_StartsWithTripleShotBloodCultistPactBearerAndHexWeaver()
    {
        List<string> titles = _warlock.entities.ConvertAll(entity => entity.title);

        CollectionAssert.AreEqual(new[] { "Triple Shot", "Blood Cultist", "Pact Bearer", "Hex Weaver" }, titles);
    }

    // Normal, Fast Shot, Chain Lightning and Random Shot left the Warlock: they are no longer among its rewards
    [TestCase("Assets/Data/Entities/NormalEntity/NormalEntity.asset")]
    [TestCase("Assets/Data/Entities/FastShootEntity/FastShootEntity.asset")]
    [TestCase("Assets/Data/Entities/ChainLightningEntity/ChainLightningEntity.asset")]
    [TestCase("Assets/Data/Entities/RandomShootEntity/RandomShootEntity.asset")]
    public void FormerWarlockUnit_HasNoWarlockTag(string path)
    {
        EntityData unit = AssetDatabase.LoadAssetAtPath<EntityData>(path);

        Assert.IsNotNull(unit, path);
        Assert.IsFalse(unit.HasTag(_warlock.classTag), unit.title);
    }

    [Test]
    public void PactBearer_IsAWarlockRewardTankWith300Health()
    {
        EntityData bearer = GetUnit("Pact Bearer");

        Assert.IsTrue(bearer.HasTag(_warlock.classTag));
        Assert.IsTrue(bearer.HasTag(TagNames.Reward));
        Assert.IsTrue(bearer.HasTag(TagNames.Tank));
        Assert.AreEqual(300f, bearer.attributes[AttributeType.HealthMax], 0.0001f);
    }

    // The Pact item: a permanent buff of two modifiers growing as the health of the holder goes down
    HPBasedModifierData GetPactModifier(AttributeType type)
    {
        ItemFactory pact = GetUnit("Pact Bearer").items[0] as ItemFactory;
        Assert.IsNotNull(pact);
        BuffHandlerFactory handler = (BuffHandlerFactory)pact.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.data.durationType);
        HPBasedModifierFactory modifier = handler.data.buffFactoryList.OfType<HPBasedModifierFactory>().FirstOrDefault(factory => factory.data.type == type);
        Assert.IsNotNull(modifier, "The Pact gives no " + type);
        // Added to the attribute: nothing at full health
        Assert.AreEqual(AttributeModifierType.Add, modifier.data.modifierType);
        return modifier.data;
    }

    // The bonus of the modifier on a holder with that much health out of 300
    static float GetPactBonus(HPBasedModifierData data, float health)
    {
        TestUnits units = new TestUnits();
        try
        {
            Entity holder = units.Create(health, 300f, "Pact Bearer");
            HPBasedModifier modifier = new HPBasedModifier { data = data };
            modifier.Init(holder.gameObject, holder.gameObject);
            return modifier.ApplyModifier();
        }
        finally
        {
            units.DestroyAll();
        }
    }

    // Armor % is a part of the damage blocked: none at full health, 70% at 0, growing linearly in between
    [TestCase(300f, 0f)]
    [TestCase(150f, 0.35f)]
    [TestCase(75f, 0.525f)]
    [TestCase(0f, 0.7f)]
    public void PactBearer_ItsArmorGrowsAsItsHealthGoesDown(float health, float expectedArmor)
    {
        HPBasedModifierData armor = GetPactModifier(AttributeType.PercentArmor);

        Assert.AreEqual(expectedArmor, GetPactBonus(armor, health), 0.0001f);
    }

    [TestCase(300f, 0f)]
    [TestCase(150f, 5f)]
    [TestCase(0f, 10f)]
    public void PactBearer_ItsDamageGrowsAsItsHealthGoesDown(float health, float expectedDamage)
    {
        HPBasedModifierData damage = GetPactModifier(AttributeType.Damage);

        Assert.AreEqual(expectedDamage, GetPactBonus(damage, health), 0.0001f);
    }

    [Test]
    public void HexWeaver_IsAWarlockRewardSupportShootingVolleysOf3()
    {
        EntityData weaver = GetUnit("Hex Weaver");

        Assert.IsTrue(weaver.HasTag(_warlock.classTag));
        Assert.IsTrue(weaver.HasTag(TagNames.Reward));
        Assert.IsTrue(weaver.HasTag(TagNames.Support));
        ShootProjectileSkillFactory shoot = weaver.skillFactories[0] as ShootProjectileSkillFactory;
        Assert.IsNotNull(shoot);
        Assert.AreEqual(3, shoot.data.projectiles[0].numberOfProjectileToShootPerTarget);
    }

    // Each hit gives the target a permanent vulnerability of 1%, stacking without limit
    [Test]
    public void HexWeaver_EachHitAddsAPermanent1PercentVulnerabilityToTheTarget()
    {
        ItemFactory hex = GetUnit("Hex Weaver").items[0] as ItemFactory;
        Assert.IsNotNull(hex);
        BuffHandlerFactory handler = (BuffHandlerFactory)hex.data.onHitEffects[0];
        Assert.AreEqual(DurationType.Infinite, handler.data.durationType);
        Assert.AreEqual(0, handler.data.maxStacks, "The vulnerability stops stacking");
        FlatModifierFactory vulnerability = handler.data.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(vulnerability);
        Assert.AreEqual(AttributeType.Vulnerability, vulnerability.data.type);
        Assert.AreEqual(AttributeModifierType.Add, vulnerability.data.modifierType);

        FlatModifier modifier = new FlatModifier { data = vulnerability.data };
        modifier.Init(null, null);
        Assert.AreEqual(0.01f, modifier.ApplyModifier(), 0.0001f);
        modifier.Stack(null, null);
        modifier.Stack(null, null);
        Assert.AreEqual(0.03f, modifier.ApplyModifier(), 0.0001f);
    }
}

}
