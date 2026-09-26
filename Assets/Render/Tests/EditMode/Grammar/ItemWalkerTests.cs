using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class ItemWalkerTests
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

    DataType CreateTracked<DataType>() where DataType : ScriptableObject
    {
        DataType instance = ScriptableObject.CreateInstance<DataType>();
        _objects.Add(instance);
        return instance;
    }

    // A handler that gives the unit's shots a behaviour, as a bounce item does
    BuffHandlerFactory CreateBehaviourHandler(AProjectileBehaviourFactory behaviour)
    {
        ProjectileBehaviourBuffFactory buff = CreateTracked<ProjectileBehaviourBuffFactory>();
        buff.data = new ProjectileBehaviourBuffData { projectileBehaviour = behaviour };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { buffFactoryList = new List<ABuffFactory> { buff } };
        return handler;
    }

    EntityData CreateUnit(ItemData item)
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = item;
        EntityData data = CreateTracked<EntityData>();
        data.items = new List<AItemFactory> { itemFactory, null };
        return data;
    }

    [Test]
    public void Buffs_ItemWithHandlers_ListsThemInOrderWithoutTheEmptyOnes()
    {
        BuffHandlerFactory first = CreateTracked<BuffHandlerFactory>();
        BuffHandlerFactory second = CreateTracked<BuffHandlerFactory>();
        EntityData data = CreateUnit(new ItemData { buffs = new List<ABuffHandlerFactory> { first, null, second } });

        List<ABuffHandlerFactory> buffs = ItemWalker.Buffs(data);

        CollectionAssert.AreEqual(new List<ABuffHandlerFactory> { first, second }, buffs);
    }

    [Test]
    public void OnHitEffects_ItemWithEffects_ListsThemApartFromTheBuffs()
    {
        BuffHandlerFactory effect = CreateTracked<BuffHandlerFactory>();
        EntityData data = CreateUnit(new ItemData { onHitEffects = new List<ABuffHandlerFactory> { effect } });

        Assert.AreEqual(1, ItemWalker.OnHitEffects(data).Count);
        Assert.AreEqual(0, ItemWalker.Buffs(data).Count);
    }

    [Test]
    public void Bounces_BounceBuffAndBehaviourItem_AddsBoth()
    {
        BounceProjectileBehaviourFactory bounce = CreateTracked<BounceProjectileBehaviourFactory>();
        bounce.data = new BounceProjectileBehaviourData { bounce = 2 };
        BuffHandlerFactory handler = CreateBehaviourHandler(bounce);
        EntityData data = CreateUnit(new ItemData
        {
            buffs = new List<ABuffHandlerFactory> { handler },
            projectileBehaviours = new List<ABuffHandlerFactory> { handler }
        });

        int bounces = ItemWalker.Bounces(data);

        Assert.AreEqual(4, bounces); // 2 through the buff, 2 through the behaviour list
    }

    [Test]
    public void Behaviours_NoDataOrNoItems_IsEmpty()
    {
        Assert.AreEqual(0, ItemWalker.Behaviours(null).Count);
        Assert.AreEqual(0, ItemWalker.Buffs(CreateTracked<EntityData>()).Count);
    }

    [TestCase(false, 100f, CountBand.One, AccessoryKind.None)]
    [TestCase(true, 300f, CountBand.Few, AccessoryKind.SmallTorus)]
    public void Channels_IncompleteItemFactories_KeepThePrimaryAndValidModifiers(bool hasCompleteModifiers,
        float health, CountBand count, AccessoryKind accessory)
    {
        BuffHandlerFactory partial = CreateTracked<BuffHandlerFactory>();
        partial.data = new BuffHandlerData
        {
            buffFactoryList = new List<ABuffFactory>
            {
                CreateTracked<FlatModifierFactory>(), CreateTracked<ProjectileBehaviourBuffFactory>(),
                CreateTracked<CurrentWaveModifierFactory>(), CreateTracked<ApplyConsumerBuffFactory>(),
                CreateTracked<DamageAllEntityOnEntityDieBuffFactory>(),
                CreateTracked<HealAllEntitiesOnRoundEndBuffFactory>(),
                CreateTracked<ManaOnRoundEndBuffFactory>(), null
            }
        };
        BuffHandlerFactory unassigned = CreateTracked<BuffHandlerFactory>();
        BuffHandlerFactory emptyBounce = CreateBehaviourHandler(CreateTracked<BounceProjectileBehaviourFactory>());
        ItemData item = new ItemData
        {
            buffs = new List<ABuffHandlerFactory> { partial, unassigned },
            projectileBehaviours = new List<ABuffHandlerFactory> { emptyBounce, unassigned }
        };
        if (hasCompleteModifiers)
        {
            FlatModifierFactory flat = CreateTracked<FlatModifierFactory>();
            flat.data = new FlatModifierData
            {
                type = AttributeType.HealthMax, modifierType = AttributeModifierType.Add, value = 200f
            };
            partial.data.buffFactoryList.Add(flat);
            BounceProjectileBehaviourFactory bounce = CreateTracked<BounceProjectileBehaviourFactory>();
            bounce.data = new BounceProjectileBehaviourData { bounce = 2 };
            item.projectileBehaviours.Add(CreateBehaviourHandler(bounce));
        }
        EntityData data = CreateUnit(item);
        data.items.Add(CreateTracked<ItemFactory>());
        data.attributes[AttributeType.HealthMax] = 100f;
        data.attributes[AttributeType.AttackRate] = 0.25f;
        ShootProjectileSkillFactory primary = CreateTracked<ShootProjectileSkillFactory>();
        primary.data = new ShootProjectileSkillData
        {
            projectiles = new List<ShootProjectileSkillData.ProjectileData>
            {
                new ShootProjectileSkillData.ProjectileData
                {
                    projectilePrefab = RenderTestAssets.LoadProjectile("StraightLaserBullet"),
                    numberOfProjectileToShootPerTarget = 1
                }
            }
        };
        data.skillFactories = new List<ASkillFactory> { primary };

        UnitChannels channels = LookDerivation.Channels(data, Entity.EntityType.Player);

        Assert.AreEqual(HeadKind.Spear, channels.head);
        Assert.AreEqual(StemBand.Quick, channels.stem);
        Assert.AreEqual(EffectFamily.Damage, channels.accent);
        Assert.AreEqual(count, channels.count);
        Assert.AreEqual(accessory, channels.accessory);
        Assert.AreEqual(health, LookDerivation.Health(data));
        Assert.AreEqual(hasCompleteModifiers ? MassBand.Heavy : MassBand.Light, channels.mass);
        Assert.AreEqual(hasCompleteModifiers ? 2 : 0, ItemWalker.Bounces(data));
        Assert.IsEmpty(EffectDerivation.Buffs(unassigned));
    }
}

}
