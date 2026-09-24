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
    public void GrassBladeMaterial_Asset_IsThePlantMaterialInTheGrassGreen()
    {
        Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
        Material plant = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");

        Assert.AreSame(plant.shader, grass.shader);
        Assert.IsTrue(grass.IsKeywordEnabled(instancedKeyword));
        CollectionAssert.AreEquivalent(plant.shaderKeywords.Append(instancedKeyword), grass.shaderKeywords);
        Assert.AreEqual(plant.enableInstancing, grass.enableInstancing);
        Assert.AreEqual(plant.renderQueue, grass.renderQueue);
        Shader shader = grass.shader;
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            string name = shader.GetPropertyName(i);
            switch (shader.GetPropertyType(i))
            {
                case ShaderPropertyType.Color:
                    if (name != "_BaseColor")
                    {
                        Assert.AreEqual(plant.GetColor(name), grass.GetColor(name), name);
                    }
                    break;
                case ShaderPropertyType.Vector:
                    Assert.AreEqual(plant.GetVector(name), grass.GetVector(name), name);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    Assert.AreEqual(plant.GetFloat(name), grass.GetFloat(name), name);
                    break;
                case ShaderPropertyType.Int:
                    Assert.AreEqual(plant.GetInteger(name), grass.GetInteger(name), name);
                    break;
                case ShaderPropertyType.Texture:
                    Assert.AreEqual(plant.GetTexture(name), grass.GetTexture(name), name);
                    break;
            }
        }
        Assert.That((Color32)grass.GetColor("_BaseColor"), Is.EqualTo(new Color32(91, 144, 85, 255))); // #5b9055
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
    public void Show_FlatPatchUnderKeyLight_ShowsItsShade()
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
        CreateGrassPatch(marked);

        _scene.Render();

        // The grass takes the global threshold, so its share follows the stones' and the heads'
        float shadeShare = LookTestScene.ShadeShare(_scene.texture);
        Assert.That(shadeShare, Is.GreaterThan(0.1f));
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
        CreateGrassPatch(AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath));
        GameObject stone = CreateStone(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat"));
        stone.transform.position = new Vector3(0.5f, 1.2f, 0.5f);
        stone.transform.localScale = Vector3.one * 1.4f;
        // Where the stone's centre falls on the carpet, along the key light
        Vector3 toLight = StageKeyLight.KeyDirection.normalized;
        Vector3 shadowCentre = stone.transform.position - toLight * ((stone.transform.position.y - 0.2f) / toLight.y);

        _scene.Render();

        Vector3 viewport = _scene.camera.WorldToViewportPoint(shadowCentre);
        int x = Mathf.RoundToInt(viewport.x * 256f);
        int y = Mathf.RoundToInt(viewport.y * 256f);
        Color32 shadow = LookTestScene.MedianColour(_scene.texture, x, y, 4);
        Color32 study = new Color32(19, 58, 113, 255); // #133a71, the 04 boulder's shadow
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
