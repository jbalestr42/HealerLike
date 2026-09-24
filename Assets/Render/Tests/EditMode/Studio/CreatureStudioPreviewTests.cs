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

namespace HealerLike.Render.Studio.Editor
{

public class CreatureStudioPreviewTests
{
    static readonly string lowPipelinePath = "Assets/Settings/Low_PipelineAsset.asset";

    readonly List<Object> _objects = new List<Object>();
    CreatureStudioPreview _preview;
    CreatureRecipe _recipe;
    RenderPipelineAsset _pipeline;
    Texture2D _capture;

    [SetUp]
    public void SetUp()
    {
        _pipeline = QualitySettings.renderPipeline;
        _preview = new CreatureStudioPreview();
        _preview.Init();
        _recipe = Track(CreatureStudioAuthoring.BuildSample(0));
    }

    [TearDown]
    public void TearDown()
    {
        _preview.Dispose();
        QualitySettings.renderPipeline = _pipeline;
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

    static void IgnoreWithoutGraphics()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Image verification requires a graphics device.");
        }
    }

    // Every part's position, scale, rotation and drawn colour
    static List<Matrix4x4> Snapshot(CreatureRig rig)
    {
        List<Matrix4x4> poses = new List<Matrix4x4>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Transform part in rig.partTransforms)
        {
            part.GetComponent<Renderer>().GetPropertyBlock(block);
            Color colour = block.GetColor("_BaseColor");
            Matrix4x4 pose = part.localToWorldMatrix;
            pose.SetRow(3, new Vector4(colour.r, colour.g, colour.b, colour.a));
            poses.Add(pose);
        }
        return poses;
    }

    static void AssertSamePoses(List<Matrix4x4> expected, List<Matrix4x4> actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            for (int cell = 0; cell < 16; cell++)
            {
                Assert.AreEqual(expected[i][cell], actual[i][cell], 0.0005f, "Part " + i);
            }
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Sample_StarterRecipe_BuildsARigOnAPrivateCopy(int index)
    {
        _recipe = Track(CreatureStudioAuthoring.BuildSample(index));
        _preview.side = index == 2 ? LookSide.Stone : LookSide.Plant;
        string before = EditorJsonUtility.ToJson(_recipe);

        CreatureRig rig = _preview.Sample(_recipe, 1.25f);

        Assert.NotNull(rig, _preview.lastError);
        Assert.AreNotSame(_recipe, rig.recipe);
        Assert.AreEqual(_recipe.parts.Length, rig.partTransforms.Count);
        Assert.IsTrue(EditorSceneManager.IsPreviewScene(rig.root.gameObject.scene));
        Assert.AreEqual(before, EditorJsonUtility.ToJson(_recipe));
        foreach (Transform part in rig.partTransforms)
        {
            Assert.AreEqual("HL/Look/Primitive", part.GetComponent<Renderer>().sharedMaterial.shader.name);
        }
    }

    [TestCase(LookSide.Plant, 1)]
    [TestCase(LookSide.Stone, 2)]
    public void Sample_EachSide_KeepsTheProductionMaterialSettings(LookSide side, int sample)
    {
        _recipe = Track(CreatureStudioAuthoring.BuildSample(sample));
        _preview.side = side;

        CreatureRig rig = _preview.Sample(_recipe, 0f);

        for (int i = 0; i < _recipe.parts.Length; i++)
        {
            string file = "Look_Default";
            if (side == LookSide.Stone)
            {
                file = "Look_Stone";
            }
            else if (_recipe.parts[i].role == PartRole.Body)
            {
                file = "Look_Body";
            }

            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/" + file + ".mat");
            Material actual = rig.partTransforms[i].GetComponent<Renderer>().sharedMaterial;
            Assert.AreNotSame(source, actual);
            Assert.AreEqual(source.GetFloat("_HLToonThresholdOffset"), actual.GetFloat("_HLToonThresholdOffset"));
            Assert.AreEqual(source.GetColor("_HLShadeTint"), actual.GetColor("_HLShadeTint"));
        }
    }

    [Test]
    public void Sample_SeekBackOrAnotherSchedule_LandsOnTheSamePose()
    {
        _preview.aim = new Vector3(1.5f, 0.8f, 0.5f);
        _preview.health = 0.62f;
        _preview.charge = 0.7f;
        _preview.glow = 0.8f;
        List<Matrix4x4> expected = Snapshot(_preview.Sample(_recipe, 1.237f));

        _preview.Sample(_recipe, 4f);
        AssertSamePoses(expected, Snapshot(_preview.Sample(_recipe, 1.237f)));
        _preview.Refresh();
        _preview.Sample(_recipe, 0.13f);
        _preview.Sample(_recipe, 0.59f);

        AssertSamePoses(expected, Snapshot(_preview.Sample(_recipe, 1.237f)));
    }

    [Test]
    public void Sample_LowerHealthAndASelectedPart_MovesThePartAndBoxesIt()
    {
        Vector3 healthy = _preview.Sample(_recipe, 0.7f).partTransforms[1].position;
        _preview.health = 0.2f;
        _preview.selectedPart = 1;

        CreatureRig wilted = _preview.Sample(_recipe, 0.7f);

        Assert.Greater(Vector3.Distance(healthy, wilted.partTransforms[1].position), 0.01f);
        GameObject box = wilted.root.parent.parent.Find("Selected Part Bounds").gameObject;
        Assert.IsTrue(box.activeSelf);
        Assert.AreEqual(12, box.transform.childCount);
        _preview.selectedPart = -1;
        _preview.Sample(_recipe, 0.7f);
        Assert.IsFalse(box.activeSelf);
    }

    [Test]
    public void Sample_RecipeBrokenAfterABuild_ReportsAndDropsTheOldRig()
    {
        Transform oldRoot = _preview.Sample(_recipe, 0.1f).root;
        _recipe.parts[0].parent = 0;
        _preview.Refresh();

        Assert.IsNull(_preview.Sample(_recipe, 0.1f));
        Assert.IsNotEmpty(_preview.lastError);
        Assert.IsTrue(oldRoot == null);
    }

    [Test]
    public void Dispose_ThreePreviews_LeavesTheOpenSceneAndTheRecipeAsTheyWere()
    {
        Scene scene = SceneManager.GetActiveScene();
        int rootCount = scene.rootCount;
        int previewScenes = EditorSceneManager.previewSceneCount;
        string before = EditorJsonUtility.ToJson(_recipe);

        for (int i = 0; i < 3; i++)
        {
            CreatureStudioPreview preview = new CreatureStudioPreview();
            preview.Init();
            preview.Sample(_recipe, 0.9f);
            preview.Sample(_recipe, 0.2f);
            preview.Dispose();
        }

        Assert.AreEqual(previewScenes, EditorSceneManager.previewSceneCount);
        Assert.AreEqual(rootCount, scene.rootCount);
        Assert.AreEqual(before, EditorJsonUtility.ToJson(_recipe));
    }

    [Test]
    public void Sample_NullRecipeThenDispose_DropsTheRigAndRefusesToSample()
    {
        Transform old = _preview.Sample(_recipe, 0f).root;

        Assert.IsNull(_preview.Sample(null, 0f));
        Assert.IsTrue(old == null);
        _preview.Dispose();
        _preview.Dispose();
        LogAssert.Expect(LogType.Error, "[CreatureStudioPreview] Sampled after Dispose");
        Assert.IsNull(_preview.Sample(_recipe, 0f));
    }

    [Test]
    public void Capture_NoRecipe_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"^\[CreatureStudioPreview\] Choose a creature"));

        _capture = _preview.Capture(null, 0f, 64, 64);

        Assert.IsNull(_capture);
    }

    [Test]
    public void Capture_EditorOnTheLowPipeline_DrawsTheLitBodyAndKeepsThePipeline()
    {
        IgnoreWithoutGraphics();
        RenderPipelineAsset low = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(lowPipelinePath);
        _recipe = Track(ScriptableObject.CreateInstance<CreatureRecipe>());
        _recipe.roots.count = 0;
        _recipe.parts = new CreaturePart[] { new CreaturePart { id = "Body", parent = -1, primitive = Primitive.Sphere,
            role = PartRole.Body, dimensions = Vector3.one, colour = RenderTestAssets.LoadPalette().plantBody } };
        _preview.isGroundShown = false;
        _preview.side = LookSide.Plant;
        QualitySettings.renderPipeline = low;

        _capture = _preview.Capture(_recipe, 0f, 256, 256);

        Assert.AreSame(low, QualitySettings.renderPipeline);
        Assert.Greater(StudioTestImages.CountGreen(_capture), 100);
    }

    [Test]
    public void Capture_Portrait_FitsTheCrownAndPutsBackTheLookGlobals()
    {
        IgnoreWithoutGraphics();
        float ink = Shader.GetGlobalFloat("_HLInkStrength");
        Vector4 tint = Shader.GetGlobalVector("_HLShadowTint");
        _preview.isGroundShown = false;

        _capture = _preview.Capture(_recipe, 0.4f, 360, 520);
        _preview.Dispose();

        Assert.AreEqual(360, _capture.width);
        Assert.AreEqual(0, StudioTestImages.CountRowsDifferent(_capture, 510, 520, 80, 280, 35));
        Assert.Greater(StudioTestImages.CountGreen(_capture), 100);
        Assert.AreEqual(ink, Shader.GetGlobalFloat("_HLInkStrength"));
        Assert.AreEqual(tint, Shader.GetGlobalVector("_HLShadowTint"));
    }
}

}
