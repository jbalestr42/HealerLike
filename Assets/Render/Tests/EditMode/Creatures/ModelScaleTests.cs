using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{

public class ModelScaleTests
{
    readonly List<Object> _owned = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object owned in _owned)
        {
            Object.DestroyImmediate(owned);
        }
        _owned.Clear();
    }

    EntityData CreateData(float rootScale)
    {
        EntityData data = ScriptableObject.CreateInstance<EntityData>();
        GameObject model = new GameObject("Model fixture");
        model.transform.localScale = Vector3.one * rootScale;
        data.model = model;
        _owned.Add(data);
        _owned.Add(model);
        return data;
    }

    [TestCase(1f)]
    [TestCase(0.9f)]
    [TestCase(0.25f)]
    [TestCase(1.0005f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-2f)]
    public void Body_RootNotLargerThanOne_IsExactlyOne(float root)
    {
        Assert.That(ModelScale.Body(Vector3.one * root), Is.EqualTo(1f));
    }

    [TestCase(1.5f, 1.5f)]
    [TestCase(2f, 2f)]
    [TestCase(40f, 4f)]
    public void Body_RootLargerThanOne_IsTheRootUpToTheCap(float root, float expected)
    {
        Assert.That(ModelScale.Body(Vector3.one * root), Is.EqualTo(expected));
    }

    [Test]
    public void Body_NoDataOrNoModel_IsExactlyOne()
    {
        EntityData noModel = ScriptableObject.CreateInstance<EntityData>();
        _owned.Add(noModel);
        Assert.That(ModelScale.Body((EntityData)null), Is.EqualTo(1f));
        Assert.That(ModelScale.Body(noModel), Is.EqualTo(1f));
    }

    // The guarantee for every unit that is not a boss: its multiplier is bit-for-bit one, so the rig is built with
    // the same cell size it had before this existed
    [Test]
    public void Body_EveryEntityAuthoredAtOrBelowOne_IsExactlyOne()
    {
        int checkedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" }))
        {
            EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null || data.model == null || data.model.transform.localScale.x > 1f + ModelScale.Tolerance)
            {
                continue;
            }
            checkedCount++;
            Assert.That(ModelScale.Body(data), Is.EqualTo(1f), data.name);
        }
        Assert.That(checkedCount, Is.GreaterThan(30));
    }

    // The fact the author states and the fact the game states agree: a unit drawn larger is a unit a boss room
    // spawns, and the other way round. A new large unit outside a boss pool, or a boss drawn at one, fails here
    // and is a conversation with the author rather than a silent resize.
    [Test]
    public void Body_LargerThanOne_OnlyForUnitsOfABossWavePool()
    {
        HashSet<EntityData> bosses = new HashSet<EntityData>();
        foreach (string guid in AssetDatabase.FindAssets("t:GameData", new[] { "Assets" }))
        {
            GameData game = AssetDatabase.LoadAssetAtPath<GameData>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (GameData.WavePool pool in game.wavePools)
            {
                if (pool.roomType != MapNodeType.Boss)
                {
                    continue;
                }
                foreach (WavePatternData wave in pool.wavePatterns)
                {
                    if (wave == null)
                    {
                        continue;
                    }
                    foreach (EntitySlot slot in wave.slots)
                    {
                        if (slot.entity != null)
                        {
                            bosses.Add(slot.entity);
                        }
                    }
                }
            }
        }
        Assert.That(bosses, Is.Not.Empty, "No boss wave in any GameData");

        foreach (string guid in AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" }))
        {
            EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(ModelScale.Body(data) > 1f, Is.EqualTo(bosses.Contains(data)), data.name);
        }
    }

    [TestCase(1f)]
    [TestCase(0.9f)]
    [TestCase(2f)]
    public void Init_ModelRootScale_SetsTheRigCellSizeFromTheAuthoredRootOnly(float root)
    {
        GameObject owner = new GameObject("Live creature");
        _owned.Add(owner);
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
        entity.data = CreateData(root);
        Material material = new Material(RenderTestAssets.LoadLookMaterial());
        _owned.Add(material);
        CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
        _owned.Add(recipe);
        CreatureBuilder builder = owner.AddComponent<CreatureBuilder>();
        RenderTestAssets.SetRecipe(builder, recipe, material, RenderTestAssets.LoadMeshes());

        builder.Init(entity);

        Assert.That(builder.rig, Is.Not.Null);
        Assert.That(builder.rig.cellSize, Is.EqualTo(StageCalibration.CellSize * ModelScale.Body(entity.data)));
        Assert.That(builder.rig.cellSize, Is.EqualTo(root > 1f ? root : StageCalibration.CellSize));
    }

    // The two normalisations together: the root still divides out the ancestor, so only the cell size grows the body
    [Test]
    public void Init_ScaledAncestorAndBodyScale_KeepsRootCalibratedAndGrowsPartsByTheScale()
    {
        GameObject plainParent = new GameObject("Plain");
        GameObject scaledParent = new GameObject("Scaled");
        scaledParent.transform.localScale = Vector3.one * 2f;
        _owned.Add(plainParent);
        _owned.Add(scaledParent);
        Material material = new Material(RenderTestAssets.LoadLookMaterial());
        _owned.Add(material);
        CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
        _owned.Add(recipe);
        CreatureRig plain = new CreatureRig();
        CreatureRig scaled = new CreatureRig();
        try
        {
            Assert.That(plain.Init(recipe, plainParent.transform, material, RenderTestAssets.LoadMeshes(), 1f), Is.True);
            Assert.That(scaled.Init(recipe, scaledParent.transform, material, RenderTestAssets.LoadMeshes(), 2f), Is.True);

            Assert.That(scaled.root.lossyScale.x, Is.EqualTo(1f).Within(0.0001f));
            float ratio = scaled.partTransforms[0].lossyScale.x / plain.partTransforms[0].lossyScale.x;
            Assert.That(ratio, Is.EqualTo(2f).Within(0.0001f));
        }
        finally
        {
            plain.Dispose();
            scaled.Dispose();
        }
    }
}
}
