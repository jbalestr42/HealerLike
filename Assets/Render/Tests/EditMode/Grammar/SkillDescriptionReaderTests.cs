using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class SkillDescriptionReaderTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object instance in _objects)
        {
            Object.DestroyImmediate(instance);
        }
        _objects.Clear();
    }

    DataType Create<DataType>() where DataType : ScriptableObject
    {
        DataType instance = ScriptableObject.CreateInstance<DataType>();
        _objects.Add(instance);
        return instance;
    }

    [Test]
    public void Read_HealingShooter_DescribesItsDeliveryCountAndOwnClock()
    {
        EntityData entity = Create<EntityData>();
        entity.attributes[AttributeType.AttackRate] = 2.5f;
        ConsumerFactory heal = Create<ConsumerFactory>();
        heal.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = -10f } } };
        ShootProjectileSkillFactory skill = Create<ShootProjectileSkillFactory>();
        GameObject prefab = RenderTestAssets.LoadProjectile("StraightLaserBullet");
        skill.data = new ShootProjectileSkillData
        {
            projectiles = new List<ShootProjectileSkillData.ProjectileData>
            {
                new ShootProjectileSkillData.ProjectileData
                {
                    projectilePrefab = prefab,
                    numberOfProjectileToShootPerTarget = 3,
                    onHitConsumer = new List<AConsumerFactory> { heal }
                }
            }
        };

        SkillDescription description = SkillDescriptionReader.Read(skill, entity);

        Assert.AreEqual(HeadKind.Spear, description.head);
        Assert.AreEqual(EffectFamily.Heal, description.accent);
        Assert.AreEqual(3, description.hits);
        Assert.AreEqual(2.5f, description.cadence);
        Assert.AreEqual(1, description.shots.Count);
        Assert.AreSame(prefab, description.shots[0].prefab);
        Assert.AreEqual(3f, description.shots[0].count);
    }

    [Test]
    public void Read_PeriodicDefenceCycle_UsesItsHandlersRatherThanAttackRate()
    {
        EntityData entity = Create<EntityData>();
        entity.attributes[AttributeType.AttackRate] = 0.1f;
        FlatModifierFactory armor = Create<FlatModifierFactory>();
        armor.data = new FlatModifierData
        {
            type = AttributeType.FlatArmor, modifierType = AttributeModifierType.Add, value = 2f
        };
        BuffHandlerFactory handler = Create<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration, duration = 3f,
            buffFactoryList = new List<ABuffFactory> { armor }
        };
        ApplyBuffPeriodicallySkillFactory skill = Create<ApplyBuffPeriodicallySkillFactory>();
        skill.data = new ApplyBuffPeriodicallySkillData
        {
            periodicBuff = new List<ABuffHandlerFactory> { handler, handler }
        };

        SkillDescription description = SkillDescriptionReader.Read(skill, entity);

        Assert.AreEqual(HeadKind.Ward, description.head);
        Assert.AreEqual(EffectFamily.Boon, description.accent);
        Assert.AreEqual(6f, description.cadence);
        Assert.AreEqual(1, description.hits);
        Assert.IsEmpty(description.shots);
    }
}

}
