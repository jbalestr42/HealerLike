using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grammar
{

// The head, accessory and accent readings of LookDerivation
public class LookDerivationHeadsTests
{
    static readonly string items = "Assets/Data/EntityItems/";

    readonly List<Object> _objects = new List<Object>();

    static ABuffHandlerFactory LoadHandler(string path)
    {
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(items + path + ".asset");
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

    // An item carrying the handlers, as the unit's EntityData lists it
    ItemFactory CreateItem(List<ABuffHandlerFactory> buffs = null, List<ABuffHandlerFactory> onHitEffects = null)
    {
        ItemFactory item = CreateTracked<ItemFactory>();
        item.data = new ItemData { buffs = buffs, onHitEffects = onHitEffects };
        return item;
    }

    // A copy that shares the asset's skills and attributes, so a test can add items without touching the asset
    EntityData Copy(EntityData source)
    {
        EntityData data = CreateTracked<EntityData>();
        data.attributes = new Dictionary<AttributeType, float>(source.attributes);
        data.skillFactories = new List<ASkillFactory>(source.skillFactories);
        return data;
    }

    // A unit whose one skill shoots a bare projectile carrying the behaviour
    EntityData ShooterWith<BehaviourType>() where BehaviourType : AProjectileBehaviour
    {
        GameObject projectileGo = new GameObject("Projectile");
        _objects.Add(projectileGo);
        projectileGo.AddComponent<BehaviourType>();
        ShootProjectileSkillFactory shoot = CreateTracked<ShootProjectileSkillFactory>();
        shoot.data = new ShootProjectileSkillData { projectiles = new List<ShootProjectileSkillData.ProjectileData>() };
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData { projectilePrefab = projectileGo });
        EntityData data = CreateTracked<EntityData>();
        data.skillFactories = new List<ASkillFactory> { shoot };
        return data;
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
        Assert.AreEqual(expected, LookDerivation.Head(LookDerivation.Primary(LookDerivationTests.LoadEntity(folder))));
    }

    [Test]
    public void Head_NoSkill_LogsAndFallsBackToBud()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"\[LookDerivation\] No head"));

        HeadKind head = LookDerivation.Head(null);

        Assert.AreEqual(HeadKind.Bud, head);
    }

    [TestCase("TripleShootEntity", HeadKind.Bud)] // the bullet entry beside two laser entries
    [TestCase("TestEntity", HeadKind.Arch)] // the curve step after the laser steps
    public void AccessoryHead_SecondDelivery_IsItsOwnHead(string folder, HeadKind expected)
    {
        EntityData data = LookDerivationTests.LoadEntity(folder);

        Assert.AreEqual(AccessoryKind.MiniHead, LookDerivation.Accessory(data));
        Assert.AreEqual(expected, LookDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_SecondSkill_IsAMiniVersionOfItsHead()
    {
        EntityData data = CreateTracked<EntityData>();
        ShootProjectileSkillFactory shoot = CreateTracked<ShootProjectileSkillFactory>();
        shoot.data = new ShootProjectileSkillData { projectiles = new List<ShootProjectileSkillData.ProjectileData>() };
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData { projectilePrefab = LookDerivationTests.LoadProjectile("BulletSpeed") });
        data.skillFactories = new List<ASkillFactory> { shoot, LookDerivationTests.LoadEntity("HitArmorBufferEntityEntity").skillFactories[0] };

        Assert.AreEqual(AccessoryKind.MiniHead, LookDerivation.Accessory(data));
        Assert.AreEqual(HeadKind.GiftBoonDefence, LookDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_RotOnHitEffect_HangsDripBeads()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("SoldierEntity"));
        ABuffHandlerFactory poison = LoadHandler("PoisonItem/BuffHandlerFactory");
        data.items = new List<AItemFactory> { CreateItem(onHitEffects: new List<ABuffHandlerFactory> { poison }) };

        Assert.AreEqual(AccessoryKind.DripBeads, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_StatPassive_IsASmallTorusForABoon()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("NormalEntity"));
        data.items = new List<AItemFactory>
        {
            CreateItem(buffs: new List<ABuffHandlerFactory> { LoadHandler("ConclaveItem/New Buff Handler Factory 1") })
        };

        Assert.AreEqual(AccessoryKind.SmallTorus, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_BackstabProjectile_IsAHook()
    {
        EntityData data = ShooterWith<BackstabProjectileBehaviour>();

        Assert.AreEqual(AccessoryKind.Hook, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_DistanceDamageProjectile_IsAnAntenna()
    {
        EntityData data = ShooterWith<IncreaseDamageOnDistanceProjectileBehaviour>();

        Assert.AreEqual(AccessoryKind.Antenna, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_CurrentWavePassive_IsTierRings()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("NormalEntity"));
        CurrentWaveModifierFactory modifier = CreateTracked<CurrentWaveModifierFactory>();
        modifier.data = new CurrentWaveModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 1f };
        BuffHandlerFactory passive = CreateTracked<BuffHandlerFactory>();
        passive.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { modifier } };
        data.items = new List<AItemFactory> { CreateItem(buffs: new List<ABuffHandlerFactory> { passive }) };

        Assert.AreEqual(AccessoryKind.TierRings, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_RenewPassive_HangsStalkBeads()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("NormalEntity"));
        data.items = new List<AItemFactory> { CreateItem(buffs: new List<ABuffHandlerFactory> { LoadHandler("RegenHpItem/RegenHpItem_BuffHandlerFactory") }) };

        Assert.AreEqual(AccessoryKind.StalkBeads, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_StatPassive_IsAConeCrownForABane()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("NormalEntity"));
        data.items = new List<AItemFactory> { CreateItem(buffs: new List<ABuffHandlerFactory> { LoadHandler("ConclaveItem/New Buff Handler Factory") }) }; // AttackRate Mul +0.2

        Assert.AreEqual(AccessoryKind.ConeCrown, LookDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_BaneOnHitEffect_HangsShardBarbs()
    {
        EntityData data = Copy(LookDerivationTests.LoadEntity("SoldierEntity"));
        data.items = new List<AItemFactory> { CreateItem(onHitEffects: new List<ABuffHandlerFactory> { LoadHandler("SlowItem/BuffHandlerFactory") }) }; // Speed Mul -0.1

        Assert.AreEqual(AccessoryKind.ShardBarbs, LookDerivation.Accessory(data));
    }
}

}
