using System.Collections.Generic;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class SpellLooksTests
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

    [Test]
    public void GetLook_MappedBuff_ReturnsTheRow()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        BuffHandlerFactory factory = CreateTracked<BuffHandlerFactory>();
        SpellLook look = new SpellLook { element = EffectElement.ManaUp };
        looks.buffs[factory] = look;

        Assert.AreSame(look, looks.GetLook(factory));
    }

    [Test]
    public void GetLook_UnmappedBuff_ReturnsNullSoTheLookIsDerived()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();

        Assert.IsNull(looks.GetLook(CreateTracked<BuffHandlerFactory>()));
        Assert.IsNull(looks.GetLook(null));
    }

    [Test]
    public void GetProjectileLook_UnmappedPrefab_DerivesItsDelivery()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        GameObject swarm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/SwarmBullet.prefab");

        Assert.AreEqual(DeliveryStyle.Swarm, looks.GetProjectileLook(swarm).style);
    }

    [Test]
    public void GetProjectileLook_MappedPrefab_ReturnsMappedLook()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        GameObject prefab = new GameObject("Projectile");
        _objects.Add(prefab);
        ProjectileLook look = new ProjectileLook { style = DeliveryStyle.Arc };
        looks.projectiles[prefab] = look;

        Assert.AreSame(look, looks.GetProjectileLook(prefab));
    }

    [Test]
    public void GetProjectileLook_NoPrefab_ReturnsDirect()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();

        Assert.AreEqual(DeliveryStyle.Direct, looks.GetProjectileLook(null).style);
    }
}

}
