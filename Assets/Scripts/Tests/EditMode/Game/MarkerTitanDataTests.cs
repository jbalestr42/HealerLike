using NUnit.Framework;
using UnityEditor;

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

    [Test]
    public void MarkedStrike_LeavesTimeToProtectTheMarkedUnit()
    {
        MarkedStrikeSkillData data = GetMarkedStrike().data;

        Assert.Greater(data.delay, 0f);
        Assert.Greater(data.interval, 0f);
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
}

}
