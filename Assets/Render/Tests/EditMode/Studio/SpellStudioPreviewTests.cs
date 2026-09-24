using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{

public class SpellStudioPreviewTests
{
    readonly List<Object> _objects = new List<Object>();
    SpellStudioPreset _preset;
    SpellStudioPreview _preview;
    Texture2D _capture;

    [SetUp]
    public void SetUp()
    {
        _preset = Track(ScriptableObject.CreateInstance<SpellStudioPreset>());
        _preset.vocabulary = RenderTestAssets.LoadEffectVocabulary();
        _preview = new SpellStudioPreview();
        _preview.Init();
    }

    [TearDown]
    public void TearDown()
    {
        _preview.Dispose();
        if (_capture != null)
        {
            Object.DestroyImmediate(_capture);
        }

        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    static Array Elements()
    {
        return Enum.GetValues(typeof(EffectElement));
    }

    // One body sphere coloured by the target's side, and a rim coloured by the caster's
    void AuthorBodyAndRim()
    {
        _preset.overrideEntry = true;
        _preset.entry = new ElementEntry();
        _preset.entry.parts = new LookPart[] { new LookPart { id = "Body colour", primitive = Primitive.Sphere,
            role = PartRole.Body, colour = ColourRole.Body, size = Vector3.one * 0.2f } };
        _preset.entry.sideRim = new LookPart[] { new LookPart { id = "Caster rim", primitive = Primitive.Sphere,
            role = PartRole.Body, colour = ColourRole.Accent, size = Vector3.one * 0.1f } };
        _preset.entry.cycleSeconds = 1f;
    }

    static Color BaseColour(Transform part)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        part.GetComponent<Renderer>().GetPropertyBlock(block);
        return block.GetColor("_BaseColor");
    }

    static Transform FindPart(SpellEffect effect, string name)
    {
        foreach (Transform part in effect.parts)
        {
            if (part.name == name)
            {
                return part;
            }
        }
        return null;
    }

    static void IgnoreWithoutGraphics()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Image verification requires a graphics device.");
        }
    }

    [TestCaseSource(nameof(Elements))]
    public void Sample_EachElement_BuildsADisabledEffectInThePreviewScene(EffectElement element)
    {
        _preset.element = element;
        _preset.critical = true;

        SpellEffect effect = _preview.Sample(_preset, _preset.previewDuration * 0.35f);

        Assert.AreEqual(element, effect.element);
        Assert.IsFalse(effect.enabled); // the timeline is the only clock
        Assert.Greater(effect.parts.Count, 0);
        Assert.IsTrue(EditorSceneManager.IsPreviewScene(effect.gameObject.scene));
        Assert.AreNotSame(_preset.vocabulary.elements[element].parts, effect.recipe.entry.parts);
        foreach (Transform part in effect.parts)
        {
            Assert.NotNull(part.GetComponent<MeshFilter>().sharedMesh, part.name);
            Assert.AreEqual("HL/Look/Primitive", part.GetComponent<Renderer>().sharedMaterial.shader.name);
            Assert.IsTrue(RenderMath.IsFinite(part.position) && RenderMath.IsFinite(part.localScale), part.name);
        }
    }

    [TestCaseSource(nameof(Elements))]
    public void Sample_SeekBack_RebuildsTheSamePose(EffectElement element)
    {
        _preset.element = element;
        _preset.tempo = EffectTempo.ForDuration;
        SpellEffect first = _preview.Sample(_preset, 0.37f);
        List<Vector3> positions = new List<Vector3>();
        foreach (Transform part in first.parts)
        {
            positions.Add(part.position);
        }

        _preview.Sample(_preset, 3.2f);
        SpellEffect rewound = _preview.Sample(_preset, 0.37f);

        Assert.AreNotSame(first, rewound); // no negative delta: the effect is built again
        Assert.AreEqual(positions.Count, rewound.parts.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            Assert.Less(Vector3.Distance(positions[i], rewound.parts[i].position), 0.0001f, "Part " + i);
        }
    }

    [Test]
    public void Sample_TickingStatusBeforeItsFirstTick_ShowsNoShape()
    {
        _preset.element = EffectElement.Rise;
        _preset.tempo = EffectTempo.PerPeriod;
        _preset.periodSeconds = 1f;

        SpellEffect before = _preview.Sample(_preset, 0.4f);
        foreach (Transform shape in before.shapes)
        {
            Assert.AreEqual(Vector3.zero, shape.localScale);
        }

        SpellEffect after = _preview.Sample(_preset, 1.3f);
        bool isShown = false;
        foreach (Transform shape in after.shapes)
        {
            isShown = isShown || (shape.gameObject.activeSelf && shape.localScale.sqrMagnitude > 0.001f);
        }
        Assert.IsTrue(isShown);
    }

    [Test]
    public void Dispose_ThreePreviews_LeavesTheOpenSceneAsItWas()
    {
        Scene scene = SceneManager.GetActiveScene();
        int rootCount = scene.rootCount;
        bool isDirty = scene.isDirty;
        int previewScenes = EditorSceneManager.previewSceneCount;

        for (int i = 0; i < 3; i++)
        {
            SpellStudioPreview preview = new SpellStudioPreview();
            preview.Init();
            preview.Sample(_preset, 0.2f);
            preview.Refresh();
            preview.Sample(_preset, 0.1f);
            preview.Dispose();
        }

        Assert.AreEqual(previewScenes, EditorSceneManager.previewSceneCount);
        Assert.AreEqual(rootCount, scene.rootCount);
        Assert.AreEqual(isDirty, scene.isDirty);
    }

    [Test]
    public void Sample_PlantThenStoneTarget_ColoursTheBodyForTheTargetAndTheRimForTheCaster()
    {
        AuthorBodyAndRim();
        LookPalette palette = _preset.vocabulary.palette;

        SpellEffect plant = _preview.Sample(_preset, 0.2f);
        Color plantColour = BaseColour(plant.parts[0]);
        _preview.target.side = LookSide.Stone;
        SpellEffect stone = _preview.Sample(_preset, 0.2f);

        Color stoneColour = BaseColour(stone.parts[0]);
        Color rim = BaseColour(FindPart(stone, "Caster rim"));
        Assert.Less(Vector4.Distance(plantColour, palette.Colour(ColourRole.Body, _preset.family, LookSide.Plant)),
            0.0001f);
        Assert.Less(Vector4.Distance(stoneColour, palette.Colour(ColourRole.Body, _preset.family, LookSide.Stone)),
            0.0001f);
        Assert.Less(Vector4.Distance(rim, palette.Colour(ColourRole.Rim, _preset.family, LookSide.Plant)), 0.0001f);
    }

    [Test]
    public void Capture_InvalidReference_LogsAndReturnsNull()
    {
        _preview.target.recipe = Track(ScriptableObject.CreateInstance<CreatureRecipe>());
        LogAssert.Expect(LogType.Error, new Regex(@"^\[SpellStudioPreview\] The reference creature needs repair"));

        _capture = _preview.Capture(_preset, 0.1f, 200, 200);

        Assert.IsNull(_capture);
        StringAssert.StartsWith("The reference creature needs repair", _preview.error);
    }

    [Test]
    public void Sample_NullPresetThenDispose_ClearsTheEffectAndRefusesToSample()
    {
        SpellEffect previous = _preview.Sample(_preset, 0.1f);

        Assert.IsNull(_preview.Sample(null, 0f));
        Assert.IsTrue(previous == null);
        _preview.Dispose();
        _preview.Dispose();
        LogAssert.Expect(LogType.Error, "[SpellStudioPreview] Sampled after Dispose");
        Assert.IsNull(_preview.Sample(_preset, 0f));
    }

    [Test]
    public void Capture_RiseAlone_RendersVisiblePixelsIntoTheCallersTexture()
    {
        IgnoreWithoutGraphics();
        _preset.element = EffectElement.Rise;
        _preset.family = EffectFamily.Heal;
        _preview.isGroundShown = false;
        _preview.isReferenceShown = false;

        _capture = _preview.Capture(_preset, _preset.previewDuration * 0.4f, 320, 240);
        _preview.Dispose();

        Assert.AreEqual(320, _capture.width);
        Assert.AreEqual(240, _capture.height);
        Assert.Greater(StudioTestImages.CountDifferent(_capture, 0, 25), 20); // the texture outlives the preview
    }

    [Test]
    public void Capture_StageCallbackDuringTheRender_PutsBackTheGlobalsAndFitsThePortrait()
    {
        IgnoreWithoutGraphics();
        float ink = Shader.GetGlobalFloat("_HLInkStrength");
        float fogStart = Shader.GetGlobalFloat("_HLFogStart");
        RenderPipelineManager.beginContextRendering += PublishStageLook;

        _capture = _preview.Capture(_preset, 0.2f, 480, 700);
        RenderPipelineManager.beginContextRendering -= PublishStageLook;

        Assert.AreEqual(ink, Shader.GetGlobalFloat("_HLInkStrength"));
        Assert.AreEqual(fogStart, Shader.GetGlobalFloat("_HLFogStart"));
        Assert.AreEqual(0, StudioTestImages.CountRowsDifferent(_capture, 690, 700, 100, 380, 35)); // the crown fits
        Assert.Greater(StudioTestImages.CountGreen(_capture), 100); // the healer keeps its greens
    }

    // What the stage does at the start of its frame: publish its own look
    static void PublishStageLook(ScriptableRenderContext context, List<Camera> cameras)
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        Shader.SetGlobalFloat("_HLFogStart", 0.01f);
    }
}

}
