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

    static SpellLooks LoadShipped()
    {
        return AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
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
    [TestCase("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory",
              EffectElement.Rise)]
    public void GetLook_ShippedKeptBuffRow_DrawsOnceWithItsElement(string path, EffectElement expected)
    {
        SpellLooks looks = LoadShipped();
        string handlerPath = "Assets/Data/" + path + ".asset";
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(handlerPath);

        SpellLook row = looks.GetLook(handler);

        Assert.AreEqual(expected, row.element);
        Assert.AreEqual(EffectTempo.Once, row.tempo);
    }

    [Test]
    public void GetLook_ShippedBuffRows_AreInfiniteHandlersDrawnOnce()
    {
        SpellLooks looks = LoadShipped();

        foreach (KeyValuePair<ABuffHandlerFactory, SpellLook> row in looks.buffs)
        {
            BuffHandlerFactory handler = row.Key as BuffHandlerFactory;
            Assert.IsNotNull(handler, row.Key.name);
            Assert.AreEqual(DurationType.Infinite, handler.data.durationType, row.Key.name);
            Assert.AreEqual(EffectTempo.Once, row.Value.tempo, row.Key.name);
        }
    }

    [Test]
    public void GetProjectileLook_ShippedRows_OnlyTheChainsKeepTheirContactPath()
    {
        SpellLooks looks = LoadShipped();
        GameObject laser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/LaserBullet.prefab");

        foreach (KeyValuePair<GameObject, ProjectileLook> row in looks.projectiles)
        {
            Assert.IsTrue(row.Value.preserveContactPath, row.Key.name);
        }
        Assert.AreEqual(DeliveryStyle.Arc, looks.GetProjectileLook(laser).style); // its baked motion is a ballistic arc
    }

    [TestCase("ChainLightning")]
    [TestCase("ChannelingLightning")]
    public void GetSpawnedLook_Chain_ReadsWhatItsRowAuthors(string prefabName)
    {
        SpellLooks looks = LoadShipped();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/" + prefabName + ".prefab");
        GameObject projectileGo = Object.Instantiate(prefab);
        _objects.Add(projectileGo);

        ProjectileLook spawned = looks.GetSpawnedLook(projectileGo.GetComponent<Projectile>());

        ProjectileLook row = looks.GetProjectileLook(prefab);
        Assert.AreEqual(row.style, spawned.style);
        Assert.AreEqual(row.presentation, spawned.presentation);
        Assert.AreEqual(row.preserveContactPath, spawned.preserveContactPath);
    }

    [TestCase("BulletSpeed", DeliveryStyle.Direct)]
    [TestCase("SwarmBullet", DeliveryStyle.Swarm)]
    [TestCase("LaserBullet", DeliveryStyle.Arc)]
    public void GetSpawnedLook_Shot_DerivesFromItsBakedBehaviours(string prefabName, DeliveryStyle expected)
    {
        GameObject projectileGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/" + prefabName + ".prefab"));
        _objects.Add(projectileGo);

        ProjectileLook look = LoadShipped().GetSpawnedLook(projectileGo.GetComponent<Projectile>());

        Assert.AreEqual(expected, look.style);
        Assert.IsFalse(look.preserveContactPath);
    }
}

}
