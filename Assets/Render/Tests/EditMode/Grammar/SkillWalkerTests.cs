using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class SkillWalkerTests
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

    GameObject CreatePrefab(string name)
    {
        GameObject prefab = new GameObject(name);
        _objects.Add(prefab);
        return prefab;
    }

    // A skill shooting each prefab the given number of times per target
    ShootProjectileSkillFactory CreateShoot(GameObject first, int firstCount, GameObject second, int secondCount)
    {
        ShootProjectileSkillFactory shoot = CreateTracked<ShootProjectileSkillFactory>();
        shoot.data = new ShootProjectileSkillData { projectiles = new List<ShootProjectileSkillData.ProjectileData>() };
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData
        {
            projectilePrefab = first,
            numberOfProjectileToShootPerTarget = firstCount
        });
        shoot.data.projectiles.Add(new ShootProjectileSkillData.ProjectileData
        {
            projectilePrefab = second,
            numberOfProjectileToShootPerTarget = secondCount
        });
        return shoot;
    }

    [Test]
    public void Shots_TwoEntries_CountsEachPrefabInDataOrder()
    {
        GameObject bullet = CreatePrefab("Bullet");
        GameObject laser = CreatePrefab("Laser");

        List<SkillWalker.Shot> shots = SkillWalker.Shots(CreateShoot(bullet, 1, laser, 3));

        Assert.AreEqual(2, shots.Count);
        Assert.AreSame(bullet, shots[0].prefab);
        Assert.AreEqual(3f, shots[1].count);
    }

    [Test]
    public void DominantPrefab_Tie_KeepsTheFirst()
    {
        GameObject bullet = CreatePrefab("Bullet");
        GameObject laser = CreatePrefab("Laser");

        Assert.AreSame(bullet, SkillWalker.DominantPrefab(CreateShoot(bullet, 2, laser, 2)));
        Assert.AreSame(laser, SkillWalker.DominantPrefab(CreateShoot(bullet, 1, laser, 2)));
    }

    [Test]
    public void Shots_SamePrefabTwice_AddsUpInOneShot()
    {
        GameObject bullet = CreatePrefab("Bullet");

        List<SkillWalker.Shot> shots = SkillWalker.Shots(CreateShoot(bullet, 1, bullet, 2));

        Assert.AreEqual(1, shots.Count);
        Assert.AreEqual(3f, shots[0].count);
    }

    [Test]
    public void Value_AttributeWithoutData_ScalesABaseOfOne()
    {
        AttributeValue attribute = new AttributeValue();
        attribute.data = new AttributeValueData { type = AttributeType.Damage, multiplier = 2f };

        Assert.AreEqual(2f, SkillWalker.Value(attribute, null));
    }

    [Test]
    public void Value_AttributeOnData_ReadsTheUnitsAttribute()
    {
        EntityData data = CreateTracked<EntityData>();
        data.attributes[AttributeType.Damage] = 5f;
        AttributeValue attribute = new AttributeValue();
        attribute.data = new AttributeValueData { type = AttributeType.Damage, multiplier = 2f };

        Assert.AreEqual(10f, SkillWalker.Value(attribute, data));
    }

    [Test]
    public void ReadAttribute_Missing_ReturnsTheFallback()
    {
        EntityData data = CreateTracked<EntityData>();

        Assert.AreEqual(7f, SkillWalker.ReadAttribute(data, AttributeType.Range, 7f));
        Assert.AreEqual(7f, SkillWalker.ReadAttribute(null, AttributeType.Range, 7f));
    }

    [Test]
    public void Bounces_PrefabWithoutBounce_IsZero()
    {
        Assert.AreEqual(0, SkillWalker.Bounces(null));
        Assert.AreEqual(0, SkillWalker.Bounces(CreatePrefab("Bullet")));
    }
}

}
