using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Look;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{

// The draw alone, then the real field's draws in the look under the stage key light
public class GrassDrawTests
{
    static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
    static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";
    static readonly string grassMaterialPath = "Assets/Render/Grass/Materials/GrassBlade.mat";
    // The keyword GrassBlade.mat carries so the look shader reads the tuft buffers
    static readonly string instancedKeyword = "HL_GRASS_INSTANCED";

    Mesh _mesh;
    Material _material;
    GrassDraw _draw;
    LookTestScene _scene;
    GrassField _patch;

    [SetUp]
    public void SetUp()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            Assert.Ignore("Indirect argument buffers need a graphics device; run with -force-metal.");
        }

        _scene = new LookTestScene();
        _scene.Init();
        _mesh = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath).tuft;
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath));
        _draw = new GrassDraw(_mesh, _material, 7, new Bounds(Vector3.zero, Vector3.one), 3);
    }

    [TearDown]
    public void TearDown()
    {
        if (_draw != null)
        {
            _draw.Release();
        }

        if (_material != null)
        {
            Object.DestroyImmediate(_material);
        }

        if (_patch != null)
        {
            _patch.Release();
        }

        if (_scene != null)
        {
            _scene.Release();
        }
    }

    [Test]
    public void Constructor_Mesh_WritesIndexAndInstanceCounts()
    {
        uint[] data = new uint[5];

        _draw.arguments.GetData(data);

        Assert.AreEqual(_mesh.GetIndexCount(0), data[0]);
        Assert.AreEqual(7u, data[1]);
        Assert.AreEqual((uint)FacetedMeshes.TuftIndexCount, data[0]); // four sides
    }

    [Test]
    public void Constructor_Material_KeepsMaterialAndCreatesProperties()
    {
        Assert.AreSame(_material, _draw.material);
        Assert.IsNotNull(_draw.properties);
    }

    [Test]
    public void Constructor_Default_CastsNoShadow()
    {
        Assert.AreEqual(ShadowCastingMode.Off, _draw.shadowCastingMode);
    }

    [Test]
    public void ShadowCastingMode_On_IsKept()
    {
        _draw.shadowCastingMode = ShadowCastingMode.On;

        Assert.AreEqual(ShadowCastingMode.On, _draw.shadowCastingMode);
    }

    [Test]
    public void Release_OwnedResources_DisposesArgumentsButNotMeshOrMaterial()
    {
        GraphicsBuffer arguments = _draw.arguments;

        _draw.Release();

        Assert.IsFalse(arguments.IsValid());
        Assert.IsTrue(_material != null);
        Assert.IsTrue(_mesh != null);
        Assert.IsNull(_draw.arguments);
    }

    [Test]
    public void GrassBladeMaterial_Asset_UsesGrassGreenWithoutInternalNormalEdges()
    {
        Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
        Material plant = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");

        Assert.AreSame(plant.shader, grass.shader);
        Assert.IsTrue(grass.IsKeywordEnabled(instancedKeyword));
        CollectionAssert.AreEquivalent(plant.shaderKeywords.Append(instancedKeyword), grass.shaderKeywords);
        Assert.AreEqual(plant.enableInstancing, grass.enableInstancing);
        Assert.AreEqual(plant.renderQueue, grass.renderQueue);
        Assert.That(grass.GetFloat("_HLNormalEdges"), Is.Zero);
        Assert.That(grass.GetFloat("_HLToonThresholdOffset"), Is.LessThan(0f));
        Assert.That(grass.GetFloat("_HLFaceHatch"), Is.GreaterThan(0.5f));
        Assert.That(grass.GetFloat("_HLHatchMultiplier"), Is.InRange(0.5f, 1f));
        Assert.That(grass.GetFloat("_HLMeadowVariation"), Is.GreaterThan(0f));
        Assert.That(grass.GetFloat("_HLGrassTipLight"), Is.GreaterThan(0f));
        Assert.That(grass.GetColor("_HLShadeTint").a, Is.Zero, "The shared blue shade must reach ordinary grass");
        Assert.That((Color32)grass.GetColor("_BaseColor"), Is.EqualTo(new Color32(97, 166, 64, 255))); // #61a640
    }

    [Test]
    public void HealRingMaterial_Asset_IsTheRingShaderWithInstancing()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");

        Shader ring = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GrassRing.shader");
        Assert.AreEqual(ring, material.shader);
        Assert.IsTrue(material.enableInstancing);
    }

    [Test]
    public void Tufts_GrassInstancingKeywords_CompileLookAndRingShaders()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Shader compilation needs a graphics device.");
        }

        foreach (string path in new[] { lookShaderPath, "Assets/Render/Shaders/GrassRing.shader" })
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.NotNull(shader);
            Material material = _scene.Track(new Material(shader) { enableInstancing = true });
            string[] shadows =
            {
                "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN"
            };
            foreach (string shadow in shadows)
            {
                foreach (bool isOctahedral in new[] { false, true })
                {
                    string normals = isOctahedral ? "_GBUFFER_NORMALS_OCT" : "";
                    material.shaderKeywords = new[]
                    {
                        "PROCEDURAL_INSTANCING_ON", "HL_GRASS_INSTANCED", shadow, normals, "_SHADOWS_SOFT"
                    };
                    for (int pass = 0; pass < material.passCount; pass++)
                    {
                        ShaderUtil.CompilePass(material, pass, true);
                        Assert.IsTrue(ShaderUtil.IsPassCompiled(material, pass), path + " pass " + pass);
                    }
                }
            }

            foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
            {
                Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
            }
        }
    }

    [Test]
    public void Show_TipLight_GradesOrdinaryBladesButLeavesHostileSpikesUnchanged()
    {
        _scene.BuildKeyLight(20f, 4f);
        _scene.camera.orthographic = true;
        _scene.camera.orthographicSize = 0.65f;
        _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -4f), Quaternion.identity);
        LookSettings settings = _scene.look.settings;
        settings.toonThreshold = 0f;
        settings.toonSoftness = 0.001f;
        settings.inkStrength = 0f;
        settings.contrast = 1f;
        _scene.look.settings = settings;
        _material.enableInstancing = true;
        _material.EnableKeyword(instancedKeyword);
        _material.SetColor("_BaseColor", new Color(0.4f, 0.6f, 0.3f));
        _draw.Release();
        _draw = new GrassDraw(_mesh, _material, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 2f),
            LookTestScene.Layer);
        using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
        using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
        using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
        {
            seeds.SetData(new[] { new TuftSeed { heightWidthLean = new Vector4(1f, 0.8f, 0f, 0f) } });
            states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
            visible.SetData(new uint[] { 0 });
            _draw.BindTufts(seeds, states, visible, 1f);
            _draw.Show(_scene.camera, () => true);
            try
            {
                _scene.Render();
                Color32 lowBefore = LookTestScene.MedianColour(_scene.texture, 128, 79, 2);
                Color32 highBefore = LookTestScene.MedianColour(_scene.texture, 128, 197, 2);
                _material.SetFloat("_HLGrassTipLight", 0.4f);
                _scene.Render();
                Color32 lowAfter = LookTestScene.MedianColour(_scene.texture, 128, 79, 2);
                Color32 highAfter = LookTestScene.MedianColour(_scene.texture, 128, 197, 2);
                Assert.That(lowBefore.g - lowAfter.g, Is.GreaterThan(8), "The blade base should be darker");
                Assert.That(highAfter.g - highBefore.g, Is.GreaterThan(4), "The blade tip should catch light");

                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 1f) } });
                _scene.Render();
                Color32[] spikeWithTipLight = _scene.texture.GetPixels32();
                _material.SetFloat("_HLGrassTipLight", 0f);
                _scene.Render();
                CollectionAssert.AreEqual(spikeWithTipLight, _scene.texture.GetPixels32(),
                    "The hostile spike must not inherit the plant root-to-tip grade");
            }
            finally
            {
                _draw.Hide();
            }
        }
    }

    [Test]
    public void Show_SpikeShadowPolicy_OrdinaryGrassReceivesLightWithoutCastingButSpikesStillCast()
    {
        _scene.BuildKeyLight(20f, 6f);
        _scene.camera.orthographic = true;
        _scene.camera.orthographicSize = 1.8f;
        _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, 0f), Quaternion.Euler(90f, 0f, 0f));
        LookSettings settings = _scene.look.settings;
        settings.inkStrength = 0f;
        _scene.look.settings = settings;
        GameObject ground = _scene.Track(GameObject.CreatePrimitive(PrimitiveType.Plane));
        ground.layer = LookTestScene.Layer;
        ground.GetComponent<Renderer>().sharedMaterial = _scene.Track(new Material(_material));
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        _material.enableInstancing = true;
        _material.EnableKeyword(instancedKeyword);
        _draw.Release();
        _draw = new GrassDraw(_mesh, _material, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 3f),
            LookTestScene.Layer);
        _draw.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
        using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
        using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
        {
            seeds.SetData(new[] { new TuftSeed { positionYaw = new Vector4(0f, 0.01f, 0f, 0f),
                heightWidthLean = new Vector4(1f, 1f, 0f, 0f) } });
            states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
            visible.SetData(new uint[] { 0 });
            _draw.BindTufts(seeds, states, visible, 1f);
            _draw.Show(_scene.camera, () => true);
            try
            {
                _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
                _scene.Render();
                int ordinaryShadow = ShadowPixels(_scene.texture);
                _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 1f);
                _scene.Render();
                int quietGround = ShadowPixels(_scene.texture);
                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 1f) } });
                _scene.Render();
                int spikeShadow = ShadowPixels(_scene.texture);
                Color32[] spikeOnly = _scene.texture.GetPixels32();
                _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
                _scene.Render();

                Debug.Log("[GrassDrawTests] Ground shadow pixels: ordinary=" + ordinaryShadow
                    + " quiet=" + quietGround + " spike=" + spikeShadow);
                Assert.That(ordinaryShadow, Is.GreaterThan(40), "The control blade must actually cast a shadow");
                Assert.That(quietGround, Is.LessThan(ordinaryShadow / 10 + 5));
                Assert.That(spikeShadow, Is.GreaterThan(6), "The hostile spike must keep a real cast shadow");
                CollectionAssert.AreEqual(spikeOnly, _scene.texture.GetPixels32(),
                    "Filtering ordinary grass must not alter the spike's shadow");
            }
            finally
            {
                _draw.Hide();
            }
        }
    }

    static int ShadowPixels(Texture2D texture)
    {
        int count = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            if (pixel.r < 200 && pixel.g < 200 && pixel.b < 200) count++;
        }
        return count;
    }

    [Test]
    public void Show_FlatPatchFromShadedSideUnderKeyLight_ShowsItsOwnShade()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsComputeShaders
            || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            Assert.Ignore("Requires compute, indirect draws and graphics readback");
        }

        // The grass teal is too green to tell from the lit blades, so a copy paints its shade magenta
        Material marked = _scene.Track(new Material(AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath)));
        marked.SetColor("_HLShadeTint", new Color(1f, 0f, 1f, 1f));
        _scene.BuildKeyLight(20f, 20f);
        // Keep the production sun, but observe the shaded side of the patch. A camera beside the sun sees
        // mostly lit faces; that old fixture depended on ordinary blades casting onto one another.
        Vector3 toLight = StageKeyLight.KeyDirection;
        float yaw = Mathf.Atan2(toLight.x, toLight.z) * Mathf.Rad2Deg;
        _scene.camera.transform.rotation = Quaternion.Euler(25f, yaw, 0f);
        _scene.camera.transform.position = -_scene.camera.transform.forward * 20f;
        CreateGrassPatch(marked);

        _scene.Render();

        float shadeShare = LookTestScene.ShadeShare(_scene.texture);
        Assert.That(shadeShare, Is.GreaterThan(0.1f));
        Assert.That(shadeShare, Is.LessThan(0.9f), "Lit faces must still retain the grass green");
        Debug.Log("[GrassDrawTests] Grass shade share " + shadeShare.ToString("F3"));
    }

    [Test]
    public void Show_StoneShadowOnGrass_ConvergesToTheGlobalUltramarine()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsComputeShaders
            || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            Assert.Ignore("Requires compute, indirect draws and graphics readback");
        }

        _scene.BuildKeyLight(20f, 20f);
        _scene.camera.transform.rotation = Quaternion.Euler(StageCalibration.PortraitPitch,
            StageCalibration.PortraitYaw, 0f);
        _scene.camera.transform.position = -_scene.camera.transform.forward * 20f;
        CreateGrassPatch(AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath));
        GameObject stone = CreateStone(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat"));
        // Lift a broad caster clear of the taller grass. Its surface must not hide the grass colour probe.
        stone.transform.position = new Vector3(0.5f, 2.5f, 0.5f);
        stone.transform.localScale = Vector3.one * 2.4f;
        stone.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        // Where the stone's centre falls on the carpet, along the key light
        Vector3 toLight = StageKeyLight.KeyDirection.normalized;
        Vector3 shadowCentre = stone.transform.position - toLight * ((stone.transform.position.y - 0.2f) / toLight.y);

        _scene.Render();

        Vector3 viewport = _scene.camera.WorldToViewportPoint(shadowCentre);
        int x = Mathf.RoundToInt(viewport.x * 256f);
        int y = Mathf.RoundToInt(viewport.y * 256f);
        Assert.That(x, Is.InRange(4, 251), "The full grass probe must be inside the captured image.");
        Assert.That(y, Is.InRange(4, 251), "The full grass probe must be inside the captured image.");
        Color32 shadow = LookTestScene.MedianColour(_scene.texture, x, y, 4);
        Color32 study = new Color32(39, 91, 127, 255); // Lifted graphic blue after the working-space contrast
        Assert.That(shadow.b - shadow.g, Is.GreaterThan(30), "The grass teal must stay out of cast shadow");
        Assert.That(Mathf.Abs(shadow.r - study.r), Is.LessThanOrEqualTo(24));
        Assert.That(Mathf.Abs(shadow.g - study.g), Is.LessThanOrEqualTo(24));
        Assert.That(Mathf.Abs(shadow.b - study.b), Is.LessThanOrEqualTo(24));
        Debug.Log("[GrassDrawTests] Stone shadow on grass " + shadow);
    }

    GameObject CreateStone(Material material)
    {
        GameObject sphere = _scene.Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        sphere.name = "Key light stone";
        sphere.layer = LookTestScene.Layer;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        return sphere;
    }

    // An eight by eight patch of the real field, its tufts drawn with the given material
    GrassField CreateGrassPatch(Material tuftMaterial)
    {
        ZoneRegistry registry = _scene.Track(new GameObject("Key light zones")).AddComponent<ZoneRegistry>();
        registry.Init();
        GrassField field = _scene.Track(new GameObject("Key light grass")).AddComponent<GrassField>();
        _patch = field;
        field.gameObject.layer = LookTestScene.Layer;
        field.Init(new Rect(-4f, -4f, 8f, 8f), 1f, 0f, _scene.camera, registry.buffer, ZonePacker.MaxZones);
        field.tuftBudget = 16384;
        ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
        Material ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");
        TestHelpers.SetPrivateField(field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath));
        TestHelpers.SetPrivateField(field, "_updateGrass", compute);
        TestHelpers.SetPrivateField(field, "_lookMaterial", tuftMaterial);
        TestHelpers.SetPrivateField(field, "_ringMaterial", ring);
        registry.PublishFrame(0f);
        field.UpdateField(registry);
        return field;
    }
}

}
