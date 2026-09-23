using System.Collections.Generic;
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

    [Test]
    public void Shipped_SeededAsset_HasEveryOutcomeAndBuffRows()
    {
        SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");

        Assert.AreEqual(20, looks.buffs.Count);
        Assert.AreEqual(9, looks.projectiles.Count);
        foreach (SpellLook look in new[] { looks.boon, looks.bane, looks.heal, looks.impact, looks.manaGain,
                                           looks.manaLoss, looks.chain, looks.shield, looks.area, looks.hostileArea })
        {
            Assert.IsNotNull(look.effectPrefab);
        }
        foreach (KeyValuePair<ABuffHandlerFactory, SpellLook> row in looks.buffs)
        {
            Assert.IsNotNull(row.Key);
            Assert.IsNotNull(row.Value.effectPrefab, row.Key.name);
        }
        foreach (KeyValuePair<GameObject, ProjectileLook> row in looks.projectiles)
        {
            Assert.IsNotNull(row.Key);
        }
    }

    [TestCase("HLStatus_Buff", HLSpellEffectKind.Buff)]
    [TestCase("HLStatus_Shield", HLSpellEffectKind.Shield)]
    [TestCase("HLFx_HealSpheres", HLSpellEffectKind.Heal)]
    [TestCase("HLFx_Impact", HLSpellEffectKind.Impact)]
    [TestCase("HLFx_ChainBeam", HLSpellEffectKind.Chain)]
    [TestCase("HLFx_HostileLitter", HLSpellEffectKind.Litter)]
    [TestCase("HLFx_HealRing", HLSpellEffectKind.Area)]
    [TestCase("HLFx_PoisonDrips", HLSpellEffectKind.Drip)]
    [TestCase("HLResolved_ManaMaxPositive", HLSpellEffectKind.Mana)]
    [TestCase("HLResolved_ManaMaxNegative", HLSpellEffectKind.Mana)]
    public void Shipped_EffectPrefab_IsAuthoredWithBakedMeshesAndTheLookMaterial(string name, HLSpellEffectKind kind)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/" + name + ".prefab");
        Material look = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");

        HLSpellEffect effect = prefab.GetComponent<HLSpellEffect>();
        SerializedObject serialized = new SerializedObject(effect);
        Assert.AreEqual(kind, effect.kind);
        Assert.Greater(effect.parts.Length, 0);
        Assert.IsNotNull(serialized.FindProperty("_sideRim").objectReferenceValue);
        Assert.AreEqual(8, serialized.FindProperty("_stackBeads").arraySize);
        Assert.AreEqual(2, serialized.FindProperty("_criticalRings").arraySize);
        Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            Assert.IsTrue(EditorUtility.IsPersistent(filter.sharedMesh), filter.name);
            StringAssert.DoesNotStartWith("Assets/Render/Spells", AssetDatabase.GetAssetPath(filter.sharedMesh));
        }
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            Assert.AreSame(look, renderer.sharedMaterial, renderer.name);
        }
    }

    [Test]
    public void Shipped_HealPrefab_HasOneStalkPerBud()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/HLFx_HealSpheres.prefab");

        HLSpellEffect effect = prefab.GetComponent<HLSpellEffect>();

        Assert.AreEqual(7, effect.parts.Length);
        Assert.AreEqual(7, effect.stalks.Length);
    }

    [Test]
    public void Shipped_Sink_UsesTheSeededLooks()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/HLSpellVisualSink.prefab");
        SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");

        HLSpellVisualSink sink = prefab.GetComponent<HLSpellVisualSink>();

        Assert.AreSame(looks, sink.looks);
    }
}
}
