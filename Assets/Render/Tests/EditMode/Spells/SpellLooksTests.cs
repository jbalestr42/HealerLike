using System.Collections.Generic;
using NUnit.Framework;
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

    SpellLooks CreateLooks()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        looks.boon = new SpellLook();
        looks.bane = new SpellLook();
        return looks;
    }

    [Test]
    public void GetLook_MappedBuff_ReturnsMappedLook()
    {
        SpellLooks looks = CreateLooks();
        BuffHandlerFactory factory = CreateTracked<BuffHandlerFactory>();
        SpellLook look = new SpellLook();
        looks.buffs[factory] = look;

        SpellLook result = looks.GetLook(factory, false);

        Assert.AreSame(look, result);
    }

    [Test]
    public void GetLook_UnmappedBuffSameSide_ReturnsBoon()
    {
        SpellLooks looks = CreateLooks();

        SpellLook result = looks.GetLook(CreateTracked<BuffHandlerFactory>(), true);

        Assert.AreSame(looks.boon, result);
    }

    [Test]
    public void GetLook_UnmappedBuffOtherSide_ReturnsBane()
    {
        SpellLooks looks = CreateLooks();

        SpellLook result = looks.GetLook(CreateTracked<BuffHandlerFactory>(), false);

        Assert.AreSame(looks.bane, result);
    }

    [Test]
    public void GetLook_NullFactory_ReturnsSideDefault()
    {
        SpellLooks looks = CreateLooks();

        SpellLook result = looks.GetLook(null, false);

        Assert.AreSame(looks.bane, result);
    }

    [Test]
    public void GetProjectileLook_MappedPrefab_ReturnsMappedLook()
    {
        SpellLooks looks = CreateLooks();
        GameObject prefab = new GameObject("Projectile");
        _objects.Add(prefab);
        ProjectileLook look = new ProjectileLook { style = HLDeliveryStyle.Arc };
        looks.projectiles[prefab] = look;

        ProjectileLook result = looks.GetProjectileLook(prefab);

        Assert.AreSame(look, result);
    }

    [Test]
    public void GetProjectileLook_UnmappedPrefab_ReturnsDirect()
    {
        SpellLooks looks = CreateLooks();

        ProjectileLook result = looks.GetProjectileLook(null);

        Assert.AreEqual(HLDeliveryStyle.Direct, result.style);
    }
}
}
