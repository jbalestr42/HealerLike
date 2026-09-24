using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grammar
{

// The head, accessory and accent readings of HeadDerivation
public class HeadDerivationTests
{
    readonly List<Object> _objects = new List<Object>();

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
        Assert.AreEqual(expected, HeadDerivation.Head(LookDerivation.Primary(RenderTestAssets.LoadEntity(folder))));
    }

    [Test]
    public void Head_NoSkill_LogsAndFallsBackToBud()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"\[HeadDerivation\] No head"));

        HeadKind head = HeadDerivation.Head(null);

        Assert.AreEqual(HeadKind.Bud, head);
    }

    [TestCase("TripleShootEntity", HeadKind.Bud)] // the bullet entry beside two laser entries
    [TestCase("TestEntity", HeadKind.Arch)] // the curve step after the laser steps
    public void AccessoryHead_SecondDelivery_IsItsOwnHead(string folder, HeadKind expected)
    {
        EntityData data = RenderTestAssets.LoadEntity(folder);

        Assert.AreEqual(AccessoryKind.MiniHead, HeadDerivation.Accessory(data));
        Assert.AreEqual(expected, HeadDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_SecondSkill_IsAMiniVersionOfItsHead()
    {
        EntityData data = CreateTracked<EntityData>();
        ShootProjectileSkillFactory shoot = CreateTracked<ShootProjectileSkillFactory>();
        shoot.data = new ShootProjectileSkillData { projectiles = new List<ShootProjectileSkillData.ProjectileData>() };
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData
            { projectilePrefab = RenderTestAssets.LoadProjectile("BulletSpeed") });
        data.skillFactories = new List<ASkillFactory>
            { shoot, RenderTestAssets.LoadEntity("HitArmorBufferEntityEntity").skillFactories[0] };

        Assert.AreEqual(AccessoryKind.MiniHead, HeadDerivation.Accessory(data));
        Assert.AreEqual(HeadKind.GiftBoonDefence, HeadDerivation.AccessoryHead(data));
    }

    [Test]
    public void Accessory_RotOnHitEffect_HangsDripBeads()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("SoldierEntity"));
        ABuffHandlerFactory poison = RenderTestAssets.LoadHandler("PoisonItem/BuffHandlerFactory");
        data.items = new List<AItemFactory> { CreateItem(onHitEffects: new List<ABuffHandlerFactory> { poison }) };

        Assert.AreEqual(AccessoryKind.DripBeads, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_StatPassive_IsASmallTorusForABoon()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("NormalEntity"));
        data.items = new List<AItemFactory>
        {
            CreateItem(buffs: new List<ABuffHandlerFactory>
                { RenderTestAssets.LoadHandler("ConclaveItem/New Buff Handler Factory 1") })
        };

        Assert.AreEqual(AccessoryKind.SmallTorus, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_BackstabProjectile_IsAHook()
    {
        EntityData data = ShooterWith<BackstabProjectileBehaviour>();

        Assert.AreEqual(AccessoryKind.Hook, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_DistanceDamageProjectile_IsAnAntenna()
    {
        EntityData data = ShooterWith<IncreaseDamageOnDistanceProjectileBehaviour>();

        Assert.AreEqual(AccessoryKind.Antenna, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_CurrentWavePassive_IsTierRings()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("NormalEntity"));
        CurrentWaveModifierFactory modifier = CreateTracked<CurrentWaveModifierFactory>();
        modifier.data = new CurrentWaveModifierData
            { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 1f };
        BuffHandlerFactory passive = CreateTracked<BuffHandlerFactory>();
        passive.data = new BuffHandlerData
            { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { modifier } };
        data.items = new List<AItemFactory> { CreateItem(buffs: new List<ABuffHandlerFactory> { passive }) };

        Assert.AreEqual(AccessoryKind.TierRings, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_RenewPassive_HangsStalkBeads()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("NormalEntity"));
        data.items = new List<AItemFactory>
        {
            CreateItem(buffs: new List<ABuffHandlerFactory>
                { RenderTestAssets.LoadHandler("RegenHpItem/RegenHpItem_BuffHandlerFactory") })
        };

        Assert.AreEqual(AccessoryKind.StalkBeads, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_StatPassive_IsAConeCrownForABane()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("NormalEntity"));
        // AttackRate Mul +0.2
        data.items = new List<AItemFactory>
        {
            CreateItem(buffs: new List<ABuffHandlerFactory>
                { RenderTestAssets.LoadHandler("ConclaveItem/New Buff Handler Factory") })
        };

        Assert.AreEqual(AccessoryKind.ConeCrown, HeadDerivation.Accessory(data));
    }

    [Test]
    public void Accessory_BaneOnHitEffect_HangsShardBarbs()
    {
        EntityData data = Copy(RenderTestAssets.LoadEntity("SoldierEntity"));
        // Speed Mul -0.1
        data.items = new List<AItemFactory>
        {
            CreateItem(onHitEffects: new List<ABuffHandlerFactory>
                { RenderTestAssets.LoadHandler("SlowItem/BuffHandlerFactory") })
        };

        Assert.AreEqual(AccessoryKind.ShardBarbs, HeadDerivation.Accessory(data));
    }
}

}
