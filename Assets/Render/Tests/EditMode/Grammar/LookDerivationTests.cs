using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grammar
{

public class LookDerivationTests
{
    static readonly string entities = "Assets/Data/Entities/";
    static readonly string projectiles = "Assets/Prefabs/Projectiles/";

    readonly List<Object> _objects = new List<Object>();

    public static EntityData LoadEntity(string folder)
    {
        string file = folder == "HitArmorBufferEntityEntity" ? "HitArmorBufferEntity" : folder;
        EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(entities + folder + "/" + file + ".asset");
        Assert.NotNull(data, folder);
        return data;
    }

    public static GameObject LoadProjectile(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectiles + name + ".prefab");
        Assert.NotNull(prefab, name);
        return prefab;
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

    // A copy that shares his skills and attributes, so a test can add passives without touching his asset
    EntityData Copy(EntityData source)
    {
        EntityData data = CreateTracked<EntityData>();
        data.attributes = new Dictionary<AttributeType, float>(source.attributes);
        data.skillFactories = new List<ASkillFactory>(source.skillFactories);
        return data;
    }

    // The spec's Part 3.1 rows for the eleven entities his data holds today
    [TestCase("NormalEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("FastShootEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Spear, CountBand.One, StemBand.Quick, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("TripleShootEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Spear, CountBand.Few, StemBand.Slow, MassBand.Light, AccessoryKind.MiniHead, EffectFamily.Damage)]
    [TestCase("MultiShotEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Arch, CountBand.One, StemBand.Quick, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("RandomShootEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Arch, CountBand.One, StemBand.Steady, MassBand.Sturdy, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("ChainLightningEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Conductor, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("ChannelingEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Fork, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("SwarmEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Arch, CountBand.Many, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("TestEntity", Entity.EntityType.Player, LookSide.Plant, HeadKind.Spear, CountBand.Many, StemBand.Slow, MassBand.Light, AccessoryKind.MiniHead, EffectFamily.Damage)]
    [TestCase("SoldierEntity", Entity.EntityType.Computer, LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Sturdy, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("HitArmorBufferEntityEntity", Entity.EntityType.Computer, LookSide.Stone, HeadKind.GiftBoonDefence, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Boon)]
    public void Channels_LiveEntity_MatchesSpecRow(string folder, Entity.EntityType entityType, LookSide side, HeadKind head,
        CountBand count, StemBand stem, MassBand mass, AccessoryKind accessory, EffectFamily accent)
    {
        UnitChannels channels = LookDerivation.Channels(LoadEntity(folder), entityType);

        Assert.AreEqual(side, channels.side);
        Assert.AreEqual(head, channels.head);
        Assert.AreEqual(count, channels.count);
        Assert.AreEqual(stem, channels.stem);
        Assert.AreEqual(mass, channels.mass);
        Assert.AreEqual(accessory, channels.accessory);
        Assert.AreEqual(accent, channels.accent);
    }

    [TestCase(Entity.EntityType.Player, LookSide.Plant)]
    [TestCase(Entity.EntityType.Computer, LookSide.Stone)]
    public void Side_EntityType_PlantForTheHealerSideStoneOtherwise(Entity.EntityType entityType, LookSide expected)
    {
        Assert.AreEqual(expected, LookDerivation.Side(entityType));
    }

    [TestCase("NormalEntity", HeadKind.Bud)]
    [TestCase("FastShootEntity", HeadKind.Spear)]
    [TestCase("TripleShootEntity", HeadKind.Spear)]
    [TestCase("MultiShotEntity", HeadKind.Arch)]
    [TestCase("RandomShootEntity", HeadKind.Arch)]
    [TestCase("ChainLightningEntity", HeadKind.Conductor)]
    [TestCase("ChannelingEntity", HeadKind.Fork)]
    [TestCase("SwarmEntity", HeadKind.Arch)]
    [TestCase("TestEntity", HeadKind.Spear)]
    [TestCase("SoldierEntity", HeadKind.Bud)]
    [TestCase("HitArmorBufferEntityEntity", HeadKind.GiftBoonDefence)]
    public void Head_PrimarySkill_ReadsTheDominantDeliveryElseTheSkillKind(string folder, HeadKind expected)
    {
        Assert.AreEqual(expected, LookDerivation.Head(LookDerivation.Primary(LoadEntity(folder))));
    }

    [Test]
    public void Head_NoSkill_LogsAndFallsBackToBud()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"\[LookDerivation\] No head"));

        HeadKind head = LookDerivation.Head(null);

        Assert.AreEqual(HeadKind.Bud, head);
    }

    [TestCase("BulletSpeed", HeadKind.Bud)]
    [TestCase("ChainLightning", HeadKind.Conductor)]
    [TestCase("ChannelingLightning", HeadKind.Fork)]
    [TestCase("CurveBullet", HeadKind.Arch)]
    [TestCase("CurveBullet2", HeadKind.Arch)]
    [TestCase("CurveSphereBullet", HeadKind.Arch)]
    [TestCase("LaserBullet", HeadKind.Arch)]
    [TestCase("StraightLaserBullet", HeadKind.Spear)]
    [TestCase("SwarmBullet", HeadKind.Arch)]
    public void Delivery_ProjectilePrefab_ReadsClassMotionAndSpeed(string name, HeadKind expected)
    {
        Assert.AreEqual(expected, LookDerivation.Delivery(LoadProjectile(name)));
    }

    [TestCase("NormalEntity", 1)]
    [TestCase("FastShootEntity", 1)]
    [TestCase("TripleShootEntity", 2)] // two per target on every entry
    [TestCase("MultiShotEntity", 1)]
    [TestCase("RandomShootEntity", 1)]
    [TestCase("ChainLightningEntity", 1)] // chains only with the Bounce item, which the body never reads
    [TestCase("ChannelingEntity", 1)]
    [TestCase("SwarmEntity", 10)]
    [TestCase("TestEntity", 5)] // 3 lasers then 2 curves in one loop
    [TestCase("SoldierEntity", 1)]
    [TestCase("HitArmorBufferEntityEntity", 1)]
    public void Hits_PrimarySkill_CountsShotsPerTrigger(string folder, int expected)
    {
        EntityData data = LoadEntity(folder);

        Assert.AreEqual(expected, LookDerivation.Hits(LookDerivation.Primary(data), data));
    }

    [TestCase(1, CountBand.One)]
    [TestCase(2, CountBand.Few)]
    [TestCase(3, CountBand.Few)]
    [TestCase(4, CountBand.Many)]
    public void Count_Hits_BandsOneFewMany(int hits, CountBand expected)
    {
        Assert.AreEqual(expected, LookDerivation.Count(hits));
    }

    [TestCase("NormalEntity", 1f)]
    [TestCase("FastShootEntity", 0.5f)]
    [TestCase("TripleShootEntity", 2f)]
    [TestCase("MultiShotEntity", 0.25f)]
    [TestCase("RandomShootEntity", 0.6f)]
    [TestCase("ChainLightningEntity", 1f)]
    [TestCase("ChannelingEntity", 1f)]
    [TestCase("SwarmEntity", 2.1f)] // 10 x 0.01 + 2, AttackRate 1
    [TestCase("TestEntity", 3.2f)] // (3 x 0.2 + 2 x 0.5) x AttackRate 2
    [TestCase("SoldierEntity", 1f)]
    [TestCase("HitArmorBufferEntityEntity", 5f)] // the support skill's rate, not its AttackRate 0
    public void Cadence_PrimarySkill_ReadsTheSkillsOwnClock(string folder, float expected)
    {
        EntityData data = LoadEntity(folder);

        Assert.AreEqual(expected, LookDerivation.Cadence(LookDerivation.Primary(data), data), 0.0001f);
    }

    [TestCase(0.25f, StemBand.Quick)]
    [TestCase(0.5f, StemBand.Quick)]
    [TestCase(0.6f, StemBand.Steady)]
    [TestCase(1.5f, StemBand.Steady)]
    [TestCase(2f, StemBand.Slow)]
    public void Stem_Cadence_BandsQuickSteadySlow(float cadence, StemBand expected)
    {
        Assert.AreEqual(expected, LookDerivation.Stem(cadence));
    }

    [TestCase("NormalEntity", 100f)]
    [TestCase("RandomShootEntity", 200f)]
    [TestCase("SoldierEntity", 150f)]
    [TestCase("HitArmorBufferEntityEntity", 100f)]
    public void Health_EntityData_ReadsHealthMax(string folder, float expected)
    {
        Assert.AreEqual(expected, LookDerivation.Health(LoadEntity(folder)), 0.0001f);
    }

    [TestCase(100f, MassBand.Light)]
    [TestCase(120f, MassBand.Light)]
    [TestCase(150f, MassBand.Sturdy)]
    [TestCase(250f, MassBand.Sturdy)]
    [TestCase(400f, MassBand.Heavy)]
    public void Mass_Health_BandsLightSturdyHeavy(float health, MassBand expected)
    {
        Assert.AreEqual(expected, LookDerivation.Mass(health));
    }

    [Test]
    public void Health_FlatHealthPassive_AddsBeforeTheBand()
    {
        EntityData data = CreateTracked<EntityData>();
        data.attributes[AttributeType.HealthMax] = 100f;
        FlatModifierFactory modifier = CreateTracked<FlatModifierFactory>();
        modifier.data = new FlatModifierData { type = AttributeType.HealthMax, modifierType = AttributeModifierType.Add, value = 200f };
        BuffHandlerFactory passive = CreateTracked<BuffHandlerFactory>();
        passive.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { modifier } };
        data.passives = new List<ABuffHandlerFactory> { passive };

        float health = LookDerivation.Health(data);

        Assert.AreEqual(300f, health, 0.0001f);
        Assert.AreEqual(MassBand.Heavy, LookDerivation.Mass(health));
    }

    [TestCase("NormalEntity")]
    [TestCase("SwarmEntity")]
    [TestCase("TestEntity")]
    [TestCase("HitArmorBufferEntityEntity")]
    public void Reach_LiveRange_IsLongOnEveryUnit(string folder)
    {
        Assert.AreEqual(ReachBand.Long, LookDerivation.Reach(LoadEntity(folder)));
    }

    [TestCase(2f, ReachBand.Short)]
    [TestCase(6f, ReachBand.Mid)]
    [TestCase(9f, ReachBand.Long)]
    public void Reach_RangeInCells_BandsShortMidLong(float range, ReachBand expected)
    {
        EntityData data = CreateTracked<EntityData>();
        data.attributes[AttributeType.Range] = range;

        Assert.AreEqual(expected, LookDerivation.Reach(data));
    }

    [TestCase("TripleShootEntity", HeadKind.Bud)] // the bullet entry beside two laser entries
    [TestCase("TestEntity", HeadKind.Arch)] // the curve step after the laser steps
    public void AccessoryHead_SecondDelivery_IsItsOwnHead(string folder, HeadKind expected)
    {
        EntityData data = LoadEntity(folder);

        Assert.AreEqual(AccessoryKind.MiniHead, LookDerivation.Accessory(data));
        Assert.AreEqual(expected, LookDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_SecondSkill_IsAMiniVersionOfItsHead()
    {
        EntityData data = CreateTracked<EntityData>();
        ShootProjectileSkillFactory shoot = CreateTracked<ShootProjectileSkillFactory>();
        shoot.data = new ShootProjectileSkillData { projectiles = new List<ShootProjectileSkillData.ProjectileData>() };
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData { projectilePrefab = LoadProjectile("BulletSpeed") });
        data.skillFactories = new List<ASkillFactory> { shoot, LoadEntity("HitArmorBufferEntityEntity").skillFactories[0] };

        Assert.AreEqual(AccessoryKind.MiniHead, LookDerivation.Accessory(data));
        Assert.AreEqual(HeadKind.GiftBoonDefence, LookDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_RotOnHitEffect_HangsDripBeads()
    {
        EntityData data = Copy(LoadEntity("SoldierEntity"));
        ABuffHandlerFactory poison = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
            "Assets/Data/EntityItems/PoisonItem/BuffHandlerFactory.asset");
        data.onHitEffects = new List<ABuffHandlerFactory> { poison };

        Assert.AreEqual(AccessoryKind.DripBeads, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_StatPassive_IsASmallTorusForABoon()
    {
        EntityData data = Copy(LoadEntity("NormalEntity"));
        data.passives = new List<ABuffHandlerFactory>
        {
            AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>("Assets/Data/EntityItems/ConclaveItem/New Buff Handler Factory 1.asset")
        };

        Assert.AreEqual(AccessoryKind.SmallTorus, LookDerivation.Accessory(data));
    }
}

}
