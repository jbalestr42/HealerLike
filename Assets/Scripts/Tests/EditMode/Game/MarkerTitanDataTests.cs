using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// The Marker Titan boss: it marks one of your units, then strikes it hard after a delay
public class MarkerTitanDataTests
{
    EntityData _titan;

    [SetUp]
    public void SetUp()
    {
        _titan = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/MarkerTitanEntity/MarkerTitanEntity.asset");
        Assert.IsNotNull(_titan);
    }

    MarkedStrikeSkillFactory GetMarkedStrike()
    {
        MarkedStrikeSkillFactory skill = (MarkedStrikeSkillFactory)_titan.skillFactories.Find(factory => factory is MarkedStrikeSkillFactory);
        Assert.IsNotNull(skill, "The Marker Titan has no marked strike");
        return skill;
    }

    // So the effects weaker on bosses (e.g. CurrentHealthDamage) know it
    [Test]
    public void IsTaggedBoss()
    {
        Assert.IsTrue(_titan.HasTag(TagNames.Boss));
    }

    [Test]
    public void MarkedStrike_LeavesTimeToProtectTheMarkedUnit()
    {
        MarkedStrikeSkillData data = GetMarkedStrike().data;

        Assert.Greater(data.delay, 0f);
        Assert.Greater(data.interval, 0f);
    }

    // The first unit takes the whole strike, the second half of it, the third a quarter
    [Test]
    public void MarkedStrike_MarksThreeUnits_TheNextOnesTakingHalfThenAQuarterOfTheStrike()
    {
        CollectionAssert.AreEqual(new[] { 1f, 0.5f, 0.25f }, GetMarkedStrike().data.targetDamageMultipliers);
    }

    [Test]
    public void MarkedStrike_ShowsTheMarkTheArcAndTheImpact()
    {
        MarkedStrikeSkillData data = GetMarkedStrike().data;

        Assert.IsNotNull(data.markerPrefab, "No marker on the marked unit");
        Assert.IsNotNull(data.arcPrefab, "No arc toward the marked unit");
        Assert.IsNotNull(data.impactPrefab, "No impact effect");
    }

    [Test]
    public void MarkedStrike_DealsAPercentOfTheMaxHealthOfTheMarkedUnit()
    {
        ConsumerFactory strike = (ConsumerFactory)GetMarkedStrike().data.strikeConsumer;

        Assert.AreEqual(ConsumerValueOwner.Target, strike.data.valueOwner);
        MaxHealthValue value = strike.data.value as MaxHealthValue;
        Assert.IsNotNull(value, "The strike isn't computed from the max health");
        Assert.AreEqual(0.8f, value.data.multiplier, 0.0001f);
        // A regular hit: armor, invincibility and shields can protect the unit
        Assert.IsFalse(strike.data.ignoreDamageReduction);
        Assert.IsFalse(strike.data.ignoreConsumerPrevention);
    }

    // The strike grows as the boss loses health: x1.5 at 0 health
    [Test]
    public void MarkedStrike_HitsHarderAsTheTitanLosesHealth()
    {
        Assert.AreEqual(0.5f, GetMarkedStrike().data.missingHealthDamageBonus, 0.0001f);
    }

    // 4 phases: the marks come every 8s, then 7s below 75% health, 6s below 50% and 5s below 25%
    [TestCase(1f, 8f)]
    [TestCase(0.7f, 7f)]
    [TestCase(0.4f, 6f)]
    [TestCase(0.1f, 5f)]
    public void TitanFury_MarksMoreOftenAtEachPhase(float healthPercent, float expectedInterval)
    {
        AItemFactory fury = _titan.items.Find(item => item != null && item.title == "Titan Fury");
        Assert.IsNotNull(fury, "The Marker Titan has no Titan Fury");
        ABuffHandlerFactory handler = ((ItemFactory)fury).data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.durationType);

        float multiplier = AttributeManager.GetDefaultValue(AttributeType.SkillCooldownMultiplier);
        foreach (ABuffFactory buff in handler.buffFactoryList)
        {
            HealthThresholdModifierData phase = ((HealthThresholdModifierFactory)buff).data;
            Assert.AreEqual(AttributeType.SkillCooldownMultiplier, phase.type);
            Assert.AreEqual(AttributeModifierType.Add, phase.modifierType);
            Assert.IsTrue(phase.isBelow);
            if (healthPercent < phase.threshold)
            {
                multiplier += phase.value;
            }
        }

        Assert.AreEqual(expectedInterval, GetMarkedStrike().data.interval * multiplier, 0.0001f);
    }

    [Test]
    public void TitanFury_HasANameAndADescription()
    {
        AItemFactory fury = _titan.items.Find(item => item != null && item.title == "Titan Fury");
        Assert.IsNotNull(fury);
        Assert.IsFalse(string.IsNullOrEmpty(((ItemFactory)fury).data.description));
    }

    [Test]
    public void Titan_IsBiggerThanTheOtherUnitsOfItsModel()
    {
        Assert.Greater(_titan.model.transform.localScale.x, 1f);
        EntityData colossus = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/ColossusEntity/ColossusEntity.asset");
        Assert.AreEqual(1f, colossus.model.transform.localScale.x, 0.0001f);
    }
}

}
