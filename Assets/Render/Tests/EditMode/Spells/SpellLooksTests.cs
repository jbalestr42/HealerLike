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

    // Only rows where the grammar is wrong for the handler stay: the three listener items on the healer, whose
    // Infinite handler would loop their look for the whole run
    [TestCase("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectElement.ManaUp)]
    [TestCase("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", EffectElement.Burst)]
    [TestCase("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory", EffectElement.Rise)]
    public void Shipped_KeptBuffRow_DrawsOnceWithItsElement(string path, EffectElement expected)
    {
        SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>("Assets/Data/" + path + ".asset");

        SpellLook row = looks.GetLook(handler);

        Assert.AreEqual(3, looks.buffs.Count);
        Assert.AreEqual(expected, row.element);
        Assert.AreEqual(EffectTempo.Once, row.tempo);
    }

    [Test]
    public void Shipped_ProjectileRows_OnlyTheChainsKeepTheirContactPath()
    {
        SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
        GameObject laser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/LaserBullet.prefab");

        Assert.AreEqual(2, looks.projectiles.Count);
        foreach (KeyValuePair<GameObject, ProjectileLook> row in looks.projectiles)
        {
            Assert.IsTrue(row.Value.preserveContactPath, row.Key.name);
        }
        Assert.AreEqual(DeliveryStyle.Arc, looks.GetProjectileLook(laser).style); // its baked motion is a ballistic arc
    }
}

}
