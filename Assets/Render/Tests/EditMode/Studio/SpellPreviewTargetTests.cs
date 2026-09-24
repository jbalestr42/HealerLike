using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{

public class SpellPreviewTargetTests
{
    readonly List<Object> _objects = new List<Object>();
    SpellPreviewTarget _target;
    SpellStudioPreset _preset;
    SpellStudioPreview _preview;

    [SetUp]
    public void SetUp()
    {
        _target = new SpellPreviewTarget();
        _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
        _preset.vocabulary = RenderTestAssets.LoadEffectVocabulary();
        _objects.Add(_preset);
        _preview = new SpellStudioPreview();
        _preview.Init();
    }

    [TearDown]
    public void TearDown()
    {
        _preview.Dispose();
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    CreatureRecipe CreateRecipe(Primitive body)
    {
        CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
        recipe.parts = new CreaturePart[] { new CreaturePart { primitive = body, role = PartRole.Body } };
        _objects.Add(recipe);
        return recipe;
    }

    [TestCase(Primitive.Stone, LookSide.Stone)]
    [TestCase(Primitive.Boulder, LookSide.Stone)]
    [TestCase(Primitive.Pyramid, LookSide.Stone)]
    [TestCase(Primitive.Sphere, LookSide.Plant)]
    public void InferSide_BodyPrimitive_ReadsTheSide(Primitive body, LookSide expected)
    {
        LookSide side = SpellPreviewTarget.InferSide(CreateRecipe(body));

        Assert.AreEqual(expected, side);
    }

    [Test]
    public void InferSide_NoRecipe_ReadsPlant()
    {
        Assert.AreEqual(LookSide.Plant, SpellPreviewTarget.InferSide(null));
    }

    [Test]
    public void Side_StoneRecipeUntilSetByHand_FollowsTheRecipe()
    {
        _target.recipe = CreateRecipe(Primitive.Stone);
        Assert.IsTrue(_target.isAutomatic);
        Assert.AreEqual(LookSide.Stone, _target.side);

        _target.side = LookSide.Plant;
        Assert.IsFalse(_target.isAutomatic);
        Assert.AreEqual(LookSide.Plant, _target.side);

        _target.isAutomatic = true;
        Assert.AreEqual(LookSide.Stone, _target.side);
    }

    [Test]
    public void Recipe_Changed_MarksTheTargetForRebuild()
    {
        CreatureRecipe recipe = CreateRecipe(Primitive.Sphere);

        _target.recipe = recipe;

        Assert.IsTrue(_target.isDirty);
        Assert.AreSame(recipe, _target.recipe);
    }

    [Test]
    public void GetAnchors_NoRig_StandsInForAUnitSizedBody()
    {
        EffectAnchors anchors = _target.GetAnchors();

        Assert.AreEqual(Vector3.up * 0.3f, anchors.bodyCentre);
        Assert.AreEqual(0.15f, anchors.headRadius);
    }

    [Test]
    public void Rebuild_PlantThenStone_DrawsWithTheProductionMaterials()
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");

        _preview.Sample(_preset, 0.2f);
        bool hasBody = false;
        foreach (Renderer renderer in _preview.target.root.GetComponentsInChildren<Renderer>())
        {
            Material material = renderer.sharedMaterial;
            hasBody = hasBody || material.name == "Spell Studio Body Material";
            Assert.That(material.name,
                Is.EqualTo("Spell Studio Body Material").Or.EqualTo("Spell Studio Preview Material"));
            if (material.name == "Spell Studio Body Material")
            {
                Assert.AreEqual(body.GetFloat("_HLToonThresholdOffset"), material.GetFloat("_HLToonThresholdOffset"));
            }
        }

        Assert.IsTrue(hasBody);
        _preview.target.side = LookSide.Stone;
        _preview.Sample(_preset, 0.2f);
        foreach (Renderer renderer in _preview.target.root.GetComponentsInChildren<Renderer>())
        {
            Assert.AreEqual("Spell Studio Stone Material", renderer.sharedMaterial.name);
        }
    }

    [Test]
    public void GetAnchors_AuthoredRecipe_PlacesTheEffectOnItsBody()
    {
        CreatureRecipe creature = CreateRecipe(Primitive.Sphere);
        creature.parts[0].id = "Authored Body";
        creature.parts[0].parent = -1;
        creature.parts[0].localPosition = Vector3.up * 2f;
        creature.parts[0].dimensions = Vector3.one;
        creature.neckLocal = Vector3.up * 2.5f;
        _preset.element = EffectElement.Burst;
        float healerHeight = _preview.Sample(_preset, 0.1f).transform.position.y;

        _preview.target.recipe = creature;
        SpellEffect authored = _preview.Sample(_preset, 0.1f);

        Assert.AreEqual(2f, authored.transform.position.y, 0.1f);
        Assert.AreEqual(Vector3.up * 2f, creature.parts[0].localPosition);
        _preview.target.recipe = null;
        Assert.AreEqual(healerHeight, _preview.Sample(_preset, 0.1f).transform.position.y, 0.001f);
    }
}

}
