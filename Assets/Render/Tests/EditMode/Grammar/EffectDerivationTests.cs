using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class EffectDerivationTests
{
    static readonly string data = "Assets/Data/";

    readonly List<Object> _objects = new List<Object>();

    static ABuffHandlerFactory Handler(string path)
    {
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(data + path + ".asset");
        Assert.NotNull(handler, path);
        return handler;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    // All twenty handler factories of the game data, with the side each one is cast on; only the poisons and the regen
    // tick
    // AttackRate Mul -0.5
    [TestCase("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", true, EffectFamily.Boon,
              EffectTempo.ForDuration, 0f)]
    // Damage Mul -0.5
    [TestCase("CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", false, EffectFamily.Bane,
              EffectTempo.ForDuration, 0f)]
    [TestCase("CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory", false, EffectFamily.Rot,
              EffectTempo.PerPeriod, 2f)]
    // AttackRate Mul +1, slower under the interval reading
    [TestCase("CharacterSkills/SingleTargetBuffAttackRate/BuffHandlerFactory", true, EffectFamily.Bane,
              EffectTempo.ForDuration, 0f)]
    // Instant HitArmor +2
    [TestCase("Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.Once, 0f)]
    [TestCase("EntityItems/BounceItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    // AttackRate Mul -0.3: a shorter attack cooldown since the item rebalance
    [TestCase("EntityItems/ConclaveItem/New Buff Handler Factory", true, EffectFamily.Boon,
              EffectTempo.ForDuration, 0f)]
    // Damage Mul +0.2
    [TestCase("EntityItems/ConclaveItem/New Buff Handler Factory 1", true, EffectFamily.Boon, EffectTempo.ForDuration,
              0f)]
    // +50% attack cooldown only: Julien's e253279c took its +10 damage out, so the trade-off is now a penalty
    [TestCase("EntityItems/ExplodeOnHitItem/BuffHandlerFactory 1", true, EffectFamily.Bane,
              EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/ExplodeOnHitItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    // Infinite Vulnerability +0.01
    [TestCase("EntityItems/IncreaseDamagePerHitItem/BuffHandlerFactory", false, EffectFamily.Bane,
              EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/IncreaseDamageWithProjectileDistanceItem/BuffHandlerFactory", true, EffectFamily.Boon,
              EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/MultipleShootItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/PoisonItem/PoisonedStacksFactory", false, EffectFamily.Rot, EffectTempo.PerPeriod, 1f)]
    [TestCase("EntityItems/RegenHpItem/RegenHpItem_BuffHandlerFactory", true, EffectFamily.Renew, EffectTempo.PerPeriod,
              1f)]
    // Speed Mul -0.1
    [TestCase("EntityItems/SlowItem/ChilledStacksFactory", false, EffectFamily.Bane, EffectTempo.ForDuration, 0f)]
    // one good, one bad, the side decides
    [TestCase("EntityItems/TrinityItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", true, EffectFamily.Damage, EffectTempo.ForDuration,
              0f)]
    [TestCase("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory", true,
              EffectFamily.Heal, EffectTempo.ForDuration, 0f)]
    [TestCase("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", true, EffectFamily.Heal,
              EffectTempo.ForDuration, 0f)]
    public void Channels_LiveHandler_ReadsFamilyGroupTempoAndPeriod(string path, bool isSameSide, EffectFamily family,
        EffectTempo tempo, float periodSeconds)
    {
        ABuffHandlerFactory handler = Handler(path);

        EffectChannels channels = EffectDerivation.Channels(handler, isSameSide);

        Assert.AreEqual(family, channels.family);
        Assert.AreEqual(tempo, channels.tempo);
        Assert.AreEqual(periodSeconds, channels.periodSeconds, 0.0001f);
    }

    [TestCase(true, EffectFamily.Boon)]
    [TestCase(false, EffectFamily.Bane)]
    public void Family_NoConsumerNoModifier_FollowsTheSide(bool isSameSide, EffectFamily expected)
    {
        ABuffHandlerFactory handler = Handler("EntityItems/MultipleShootItem/BuffHandlerFactory");

        Assert.AreEqual(expected, EffectDerivation.Family(handler, isSameSide));
    }

    [TestCase(10f, false, EffectFamily.Damage)]
    [TestCase(-2f, false, EffectFamily.Heal)]
    [TestCase(10f, true, EffectFamily.Rot)]
    [TestCase(-4f, true, EffectFamily.Renew)]
    public void ConsumerFamily_ValueSign_HarmIsPositive(float value, bool isPeriodic, EffectFamily expected)
    {
        ConsumerFactory consumer = CreateTracked<ConsumerFactory>();
        FlatValue flat = new FlatValue();
        flat.data = new FlatValueData { value = value };
        consumer.data = new ConsumerData { value = flat };

        Assert.AreEqual(expected, EffectDerivation.ConsumerFamily(consumer, isPeriodic));
    }

    // Multiplied on a wounded target, it still deals its value: read as that value, without an error
    [TestCase(10f, EffectFamily.Damage)]
    [TestCase(-2f, EffectFamily.Heal)]
    public void Harm_WoundedTargetConsumer_ReadsItsValue(float value, EffectFamily expected)
    {
        WoundedTargetConsumerFactory consumer = CreateTracked<WoundedTargetConsumerFactory>();
        consumer.data = new WoundedTargetConsumerData { value = new FlatValue { data = new FlatValueData { value = value } } };

        Assert.AreEqual(value, EffectDerivation.Harm(consumer));
        Assert.AreEqual(expected, EffectDerivation.ConsumerFamily(consumer, false));
    }

    // A status given by stacks: read as the buff of the status itself, here a periodic damage
    [Test]
    public void Channels_StatusStacks_ReadTheBuffOfTheStatus()
    {
        ConsumerFactory consumer = CreateTracked<ConsumerFactory>();
        consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 4f } } };
        ApplyConsumerBuffFactory buff = CreateTracked<ApplyConsumerBuffFactory>();
        buff.data = new ApplyConsumerBuffData { consumerFactory = consumer };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            isPeriodic = true,
            periodDuration = 1f,
            buffFactoryList = new List<ABuffFactory> { buff }
        };
        StatusStacksFactory stacks = StatusStacks(handler, 1);

        Assert.AreEqual(EffectFamily.Rot, EffectDerivation.Family(stacks, false));
        Assert.AreEqual(EffectTempo.PerPeriod, EffectDerivation.Tempo(stacks));
        Assert.AreEqual(1f, EffectDerivation.Period(stacks));
    }

    // Hasted speeds up the attack cooldown: a better (lower) attack rate, drawn as a rate change
    [Test]
    public void AttackSpeedModifier_IsABoonOnTheAttackRateOfKindRate()
    {
        BuffHandlerFactory handler = AttackSpeedHandler(0.25f);

        Assert.IsTrue(EffectDerivation.TryModifier(handler.data.buffFactoryList[0], out AttributeType type, out float delta));
        Assert.AreEqual(AttributeType.AttackRate, type);
        Assert.Less(delta, 0f);
        Assert.AreEqual(EffectFamily.Boon, EffectDerivation.Family(handler, false));
        Assert.AreEqual(EffectKind.Rate, EffectDerivation.Kind(handler));
    }

    // The stacks given at once add up: several stacks of a light status are no longer light
    [Test]
    public void Magnitude_StatusStacks_GrowsWithTheStacksGiven()
    {
        BuffHandlerFactory handler = AttackSpeedHandler(0.1f);

        Assert.AreEqual(EffectMagnitude.Light, EffectDerivation.Magnitude(StatusStacks(handler, 1)));
        Assert.AreNotEqual(EffectMagnitude.Light, EffectDerivation.Magnitude(StatusStacks(handler, 3)));
    }

    BuffHandlerFactory AttackSpeedHandler(float speed)
    {
        AttackSpeedModifierFactory modifier = CreateTracked<AttackSpeedModifierFactory>();
        modifier.data = new AttackSpeedModifierData
        {
            type = AttributeType.AttackRate,
            modifierType = AttributeModifierType.Multiply,
            value = speed
        };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 5f,
            buffFactoryList = new List<ABuffFactory> { modifier }
        };
        return handler;
    }

    StatusStacksFactory StatusStacks(ABuffHandlerFactory handler, int count)
    {
        StatusData status = CreateTracked<StatusData>();
        status.buffHandlerFactory = handler;
        StatusStacksFactory stacks = CreateTracked<StatusStacksFactory>();
        stacks.data = new StatusStacksData { status = status, stacks = count };
        return stacks;
    }

    [TestCase(AttributeType.AttackRate, -1f)]
    [TestCase(AttributeType.Vulnerability, -1f)]
    [TestCase(AttributeType.SkillCooldownMultiplier, -1f)]
    [TestCase(AttributeType.SkillCostMultiplier, -1f)]
    [TestCase(AttributeType.Damage, 1f)]
    [TestCase(AttributeType.HitArmor, 1f)]
    [TestCase(AttributeType.HealingReceived, 1f)]
    public void Polarity_Attribute_LessIsBetterForIntervalVulnerabilityAndSkillCooldownAndCost(AttributeType type,
                                                                                               float expected)
    {
        Assert.AreEqual(expected, EffectDerivation.Polarity(type));
    }

    [TestCase("Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", AttributeGroup.Defence)]
    [TestCase("EntityItems/IncreaseDamagePerHitItem/BuffHandlerFactory", AttributeGroup.Defence)]
    [TestCase("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", AttributeGroup.Offence)]
    [TestCase("CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", AttributeGroup.Offence)]
    public void Group_ModifiedAttribute_DefenceOrOffence(string path, AttributeGroup expected)
    {
        Assert.AreEqual(expected, EffectDerivation.Group(Handler(path)));
    }

    // Verdant heals every player entity each period: its consumer sits on the buff, not on an ApplyConsumer buff
    [Test]
    public void Consumer_ApplyConsumerOnEntities_ReadsItsConsumerAsAPeriodicHeal()
    {
        ConsumerFactory consumer = CreateTracked<ConsumerFactory>();
        consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = -3f } } };
        ApplyConsumerOnEntitiesBuffFactory buff = CreateTracked<ApplyConsumerOnEntitiesBuffFactory>();
        buff.data = new ApplyConsumerOnEntitiesBuffData { consumerFactory = consumer };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            isPeriodic = true,
            periodDuration = 2f,
            buffFactoryList = new List<ABuffFactory> { buff }
        };

        Assert.AreSame(consumer, EffectDerivation.Consumer(buff));
        Assert.AreEqual(EffectFamily.Renew, EffectDerivation.Family(handler, false));
        Assert.AreEqual(EffectOperation.Heal, EffectDerivation.Operation(handler, false));
        Assert.AreEqual(EffectTempo.PerPeriod, EffectDerivation.Tempo(handler));
    }

    // Life steal has no consumer asset, yet what it does is heal the most wounded ally, on either side
    [TestCase(true)]
    [TestCase(false)]
    public void Family_LifeSteal_IsAHealWhateverTheSide(bool isSameSide)
    {
        LifeStealBuffFactory buff = CreateTracked<LifeStealBuffFactory>();
        buff.data = new LifeStealBuffData { ratio = 0.5f };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { buff }
        };

        Assert.AreEqual(EffectFamily.Heal, EffectDerivation.Family(handler, isSameSide));
        Assert.AreEqual(EffectOperation.Heal, EffectDerivation.Operation(handler, isSameSide));
        Assert.AreEqual(EffectKind.Plain, EffectDerivation.Kind(handler));
    }

    [Test]
    public void Group_Invincibility_IsPrevention()
    {
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 2f,
            buffFactoryList = new List<ABuffFactory> { CreateTracked<InvincibilityBuffFactory>() }
        };

        Assert.AreEqual(AttributeGroup.Prevention, EffectDerivation.Group(handler));
        Assert.AreEqual(EffectFamily.Boon, EffectDerivation.Family(handler, true));
    }

    // The nine projectile prefabs; LaserBullet's authored row says Rigid, its baked arc motion says Arc
    [TestCase("BulletSpeed", DeliveryStyle.Direct)]
    [TestCase("ChainLightning", DeliveryStyle.ChainSync)]
    [TestCase("ChannelingLightning", DeliveryStyle.ChainSync)]
    [TestCase("CurveBullet", DeliveryStyle.Arc)]
    [TestCase("CurveBullet2", DeliveryStyle.Arc)]
    [TestCase("CurveSphereBullet", DeliveryStyle.Arc)]
    [TestCase("LaserBullet", DeliveryStyle.Arc)]
    [TestCase("StraightLaserBullet", DeliveryStyle.Rigid)]
    [TestCase("SwarmBullet", DeliveryStyle.Swarm)]
    public void Delivery_ProjectilePrefab_AgreesWithTheHead(string name, DeliveryStyle expected)
    {
        Assert.AreEqual(expected, EffectDerivation.Delivery(RenderTestAssets.LoadProjectile(name)));
    }

    // A spawned projectile carries no link to its prefab, so its delivery is read from the behaviours baked in it
    [TestCase("BulletSpeed", DeliveryStyle.Direct)]
    [TestCase("SwarmBullet", DeliveryStyle.Swarm)]
    [TestCase("LaserBullet", DeliveryStyle.Arc)]
    [TestCase("ChainLightning", DeliveryStyle.ChainSync)]
    [TestCase("ChannelingLightning", DeliveryStyle.ChainSync)]
    public void Delivery_SpawnedProjectile_ReadsItsBakedBehaviours(string name, DeliveryStyle expected)
    {
        GameObject projectileGo = Object.Instantiate(RenderTestAssets.LoadProjectile(name));

        DeliveryStyle style = EffectDerivation.Delivery(projectileGo);

        Object.DestroyImmediate(projectileGo);
        Assert.AreEqual(expected, style);
    }
}

}
