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

    // All twenty handler factories of the game data, with the side each one is cast on; only the poisons and the regen tick
    [TestCase("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)] // AttackRate Mul -0.5
    [TestCase("CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", false, EffectFamily.Bane, EffectTempo.ForDuration, 0f)] // Damage Mul -0.5
    [TestCase("CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory", false, EffectFamily.Rot, EffectTempo.PerPeriod, 2f)]
    [TestCase("CharacterSkills/SingleTargetBuffAttackRate/BuffHandlerFactory", true, EffectFamily.Bane, EffectTempo.ForDuration, 0f)] // AttackRate Mul +1, slower under the interval reading
    [TestCase("Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.Once, 0f)] // Instant HitArmor +2
    [TestCase("EntityItems/BounceItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/ConclaveItem/New Buff Handler Factory", true, EffectFamily.Bane, EffectTempo.ForDuration, 0f)] // AttackRate Mul +0.2
    [TestCase("EntityItems/ConclaveItem/New Buff Handler Factory 1", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)] // Damage Mul +0.2
    [TestCase("EntityItems/ExplodeOnHitItem/BuffHandlerFactory 1", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)] // Damage +10, AttackRate Mul -0.5
    [TestCase("EntityItems/ExplodeOnHitItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/IncreaseDamagePerHitItem/BuffHandlerFactory", false, EffectFamily.Bane, EffectTempo.ForDuration, 0f)] // Infinite Vulnerability +0.01
    [TestCase("EntityItems/IncreaseDamageWithProjectileDistanceItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/MultipleShootItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)]
    [TestCase("EntityItems/PoisonItem/BuffHandlerFactory", false, EffectFamily.Rot, EffectTempo.PerPeriod, 1.5f)]
    [TestCase("EntityItems/RegenHpItem/RegenHpItem_BuffHandlerFactory", true, EffectFamily.Renew, EffectTempo.PerPeriod, 2f)]
    [TestCase("EntityItems/SlowItem/BuffHandlerFactory", false, EffectFamily.Bane, EffectTempo.ForDuration, 0f)] // Speed Mul -0.1
    [TestCase("EntityItems/TrinityItem/BuffHandlerFactory", true, EffectFamily.Boon, EffectTempo.ForDuration, 0f)] // one good, one bad, the side decides
    [TestCase("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", true, EffectFamily.Damage, EffectTempo.ForDuration, 0f)]
    [TestCase("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory", true, EffectFamily.Heal, EffectTempo.ForDuration, 0f)]
    [TestCase("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", true, EffectFamily.Heal, EffectTempo.ForDuration, 0f)]
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

    [TestCase(AttributeType.AttackRate, -1f)]
    [TestCase(AttributeType.Vulnerability, -1f)]
    [TestCase(AttributeType.Damage, 1f)]
    [TestCase(AttributeType.HitArmor, 1f)]
    public void Polarity_Attribute_LessIsBetterForIntervalAndVulnerability(AttributeType type, float expected)
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
