using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using HealerLike.Render.Look;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class GrassFieldTests
{
    static readonly Rect oneCell = new Rect(-0.5f, -0.5f, 1f, 1f);

    GameObject _go;
    GrassField _field;
    GraphicsBuffer _borrowedZones;

    // Only the opt-in capture touches these: its scene objects and the global render state it borrows
    readonly List<Object> _owned = new List<Object>();
    readonly List<LookController> _disabledLooks = new List<LookController>();
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    RenderTexture _previousTarget;

    static bool HasGraphicsDevice()
    {
        return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders && SystemInfo.supportsIndirectArgumentsBuffer;
    }

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("GrassFieldTest");
        _field = _go.AddComponent<GrassField>();
        _previousPipeline = QualitySettings.renderPipeline;
        _previousSun = RenderSettings.sun;
        _previousTarget = RenderTexture.active;
    }

    [TearDown]
    public void TearDown()
    {
        if (_field != null)
        {
            _field.Release();
        }
        Object.DestroyImmediate(_go);
        if (_borrowedZones != null)
        {
            _borrowedZones.Dispose();
            _borrowedZones = null;
        }

        RenderTexture.active = _previousTarget;
        QualitySettings.renderPipeline = _previousPipeline;
        RenderSettings.sun = _previousSun;
        for (int i = _owned.Count - 1; i >= 0; i--)
        {
            if (_owned[i])
            {
                Object.DestroyImmediate(_owned[i]);
            }
        }
        _owned.Clear();
        foreach (LookController look in _disabledLooks)
        {
            if (look)
            {
                look.enabled = true;
            }
        }
        _disabledLooks.Clear();
    }

    T Track<T>(T item) where T : Object
    {
        _owned.Add(item);
        return item;
    }

    GameObject CreateCaptureObject(string name)
    {
        GameObject go = Track(new GameObject(name));
        go.layer = 30;
        return go;
    }

    static int GreenPixels(Texture2D texture)
    {
        return texture.GetPixels32().Count(c => c.g > 140 && c.g > c.r * 1.1f && c.g > c.b * 1.3f);
    }

    static void Render(Camera camera, RenderTexture target, Texture2D texture)
    {
        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0f, 0f, 1440f, 960f), 0, 0);
        texture.Apply();
    }

    void SetAssets()
    {
        TestHelpers.SetPrivateField(_field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset"));
        TestHelpers.SetPrivateField(_field, "_updateGrass", AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute"));
        TestHelpers.SetPrivateField(_field, "_lookMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat"));
        TestHelpers.SetPrivateField(_field, "_ringMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat"));
    }

    void BuildOneCellField()
    {
        if (_borrowedZones == null)
        {
            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassField.MaxZones, Zone.Stride);
        }

        _field.Init(oneCell, 1f, 0.5f, null, _borrowedZones, GrassField.MaxZones);
        _field.bladeBudget = 65;
        SetAssets();

        MethodInfo build = typeof(GrassField).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);
        GrassBuildKey key = new GrassBuildKey(oneCell, 1f, 0.5f, 1, _field.bladeBudget);
        Assert.IsTrue((bool)build.Invoke(_field, new object[] { key }));
    }

    List<GraphicsBuffer> OwnedBuffers()
    {
        List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
        foreach (FieldInfo member in typeof(GrassField).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            GraphicsBuffer buffer = member.GetValue(_field) as GraphicsBuffer;
            if (buffer != null && !ReferenceEquals(buffer, _borrowedZones))
            {
                buffers.Add(buffer);
            }
        }
        buffers.Add(_field.bladeDraw.arguments);
        buffers.Add(_field.ringDraw.arguments);
        return buffers;
    }

    [Test]
    public void BladeBudget_Default_IsLayoutDefault()
    {
        Assert.AreEqual(GrassLayout.DefaultBudget, _field.bladeBudget);
    }

    [Test]
    public void BladeBudget_OutsideRange_Clamps()
    {
        _field.bladeBudget = int.MaxValue;
        Assert.AreEqual(GrassLayout.MaxBudget, _field.bladeBudget);

        _field.bladeBudget = -1;
        Assert.AreEqual(0, _field.bladeBudget);
    }

    [Test]
    public void BladeHeightScale_OutsideRange_ClampsAndResetsNaN()
    {
        _field.bladeHeightScale = 0.8f;
        Assert.AreEqual(0.8f, _field.bladeHeightScale);

        _field.bladeHeightScale = 3f;
        Assert.AreEqual(1f, _field.bladeHeightScale);

        _field.bladeHeightScale = -1f;
        Assert.AreEqual(0.25f, _field.bladeHeightScale);

        _field.bladeHeightScale = float.NaN;
        Assert.AreEqual(1f, _field.bladeHeightScale);
    }

    [Test]
    public void SetZoneSnapshot_CountWithoutBuffer_KeepsPreviousSnapshot()
    {
        _field.SetZoneSnapshot(null, 0);

        TestHelpers.WithLoggingDisabled(() => _field.SetZoneSnapshot(null, 1));
        TestHelpers.WithLoggingDisabled(() => _field.SetZoneCount(GrassField.MaxZones + 1));

        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void Init_WithoutZoneBuffer_LogsAndStaysUnbuilt()
    {
        LogAssert.Expect(LogType.Error, "[GrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.");

        _field.Init(oneCell, 1f, 0.5f, null, null, GrassField.MaxZones);
        _field.UpdateField(null, 0);

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.bladeCount);
    }

    [Test]
    public void Init_NonFiniteCellSize_LogsAndStaysUnbuilt()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassField.MaxZones, Zone.Stride);
        LogAssert.Expect(LogType.Error, new Regex(@"^\[GrassField\] Rejected area .* with cell size NaN"));

        _field.Init(oneCell, float.NaN, 0.5f, null, _borrowedZones, GrassField.MaxZones);

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void Release_CalledTwice_StaysReleased()
    {
        _field.Release();
        _field.Release();

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.bladeCount);
        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void TriggerGust_TowardTarget_TurnsTheWind()
    {
        _field.TriggerGust(Vector3.forward);

        Assert.AreEqual(1f, _field.wind.current.y);
    }

    [Test]
    public void UpdateField_WithCamera_BuildsFromTheInitArea()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        Camera camera = _go.AddComponent<Camera>();
        _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassField.MaxZones, Zone.Stride);
        _field.Init(new Rect(2f, -3f, 4f, 2f), 1f, 0.5f, camera, _borrowedZones, GrassField.MaxZones);
        _field.bladeBudget = 80;
        SetAssets();

        _field.UpdateField(_borrowedZones, 3);

        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(80, _field.bladeCount);
        Assert.AreEqual(3, _field.activeZoneCount);
        Assert.AreEqual(new Vector3(4f, 0.505f, -2f), BladeBoundsCenter()); // area centre, surface plus root lift
    }

    Vector3 BladeBoundsCenter()
    {
        FieldInfo key = typeof(GrassField).GetField("_builtKey", BindingFlags.Instance | BindingFlags.NonPublic);
        return ((GrassBuildKey)key.GetValue(_field)).CalculateBounds().center;
    }

    [Test]
    public void Build_OnGraphicsDevice_DrawsOneConeListWithTheLookShader()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        BuildOneCellField();

        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(65, _field.bladeCount);
        Assert.AreEqual("HL/Look/Primitive", _field.bladeDraw.material.shader.name);
        Assert.IsTrue(_field.bladeDraw.material.IsKeywordEnabled(GrassPalette.InstancedKeyword));
        uint[] data = new uint[5];
        _field.bladeDraw.arguments.GetData(data);
        Assert.AreEqual(9u * (uint)GrassField.BladeSides, data[0]); // one cone: side quads and base cap
        Assert.AreEqual(5, OwnedBuffers().Count); // seeds, states, visible ids, blade and ring arguments
        Assert.AreEqual(ShadowCastingMode.On, _field.bladeDraw.shadowCastingMode);
        Assert.AreEqual(ShadowCastingMode.Off, _field.ringDraw.shadowCastingMode);
    }

    [Test]
    public void OnDisable_BuiltField_ReleasesOwnedBuffersAndKeepsBorrowedZonesAndMaterials()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        for (int cycle = 0; cycle < 3; cycle++)
        {
            BuildOneCellField();
            List<GraphicsBuffer> buffers = OwnedBuffers();
            Material bladeMaterial = _field.bladeDraw.material;
            Material ringMaterial = _field.ringDraw.material;

            // Runtime messages do not run on their own in EditMode
            TestHelpers.InvokePrivate(_field, "OnDisable");

            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsFalse(buffer.IsValid());
            }
            Assert.IsTrue(bladeMaterial != null);
            Assert.IsTrue(ringMaterial != null);
            Assert.IsTrue(_borrowedZones.IsValid());
            Assert.IsFalse(_field.isReady);
            Assert.IsNull(_field.bladeDraw);
        }
    }

    [Test]
    public void OnDestroy_BuiltField_ReleasesOwnedResources()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }
        BuildOneCellField();
        List<GraphicsBuffer> buffers = OwnedBuffers();

        TestHelpers.InvokePrivate(_field, "OnDestroy");
        Object.DestroyImmediate(_field);

        foreach (GraphicsBuffer buffer in buffers)
        {
            Assert.IsFalse(buffer.IsValid());
        }
        Assert.IsTrue(_borrowedZones.IsValid());
    }

    [UnityTest]
    public IEnumerator UpdateField_CaptureOnMetal_DrawsCarpetAcrossRepaintsUntilSnapshotRevoked()
    {
        if (System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_GROUND") != "1" || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Opt-in visual fixture: RENDER_CAPTURE_GROUND=1 with Metal.");
        }

        foreach (LookController owner in Object.FindObjectsByType<LookController>(FindObjectsSortMode.None))
        {
            if (owner.enabled)
            {
                owner.enabled = false;
                _disabledLooks.Add(owner);
            }
        }
        QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Very High_PipelineAsset.asset");
        Camera camera = CreateCaptureObject("GroundFixtureCamera").AddComponent<Camera>();
        camera.cullingMask = 1 << 30;
        camera.fieldOfView = 44f;
        camera.aspect = 1.5f;
        camera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
        camera.transform.position = -camera.transform.forward * 12f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.74f, 0.82f, 0.83f);
        Light light = CreateCaptureObject("GroundFixtureKey").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        light.shadows = LightShadows.Soft;
        light.intensity = 1f;
        RenderSettings.sun = light;
        LookController look = CreateCaptureObject("GroundFixtureLook").AddComponent<LookController>();
        LookSettings settings = LookSettings.Default;
        settings.fogStart = 25f;
        settings.fogEnd = 60f;
        settings.shadowTint = new Color32(63, 91, 148, 255);
        settings.inkStrength = 0.75f;
        look.settings = settings;
        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader")));
        material.SetColor("_BaseColor", ((Color)new Color32(78, 126, 87, 255)).linear);
        GameObject ground = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        ground.layer = 30;
        ground.transform.localScale = new Vector3(8f, 0.2f, 8f);
        ground.transform.position = Vector3.down * 0.1f;
        ground.GetComponent<Renderer>().sharedMaterial = material;
        ZoneRegistry registry = CreateCaptureObject("GroundFixtureZones").AddComponent<ZoneRegistry>();
        registry.Init();
        GrassField field = CreateCaptureObject("GroundFixtureGrass").AddComponent<GrassField>();
        field.Init(new Rect(-4f, -4f, 8f, 8f), 1f, 0f, camera, registry.buffer, 64);
        field.bladeBudget = 16384;
        TestHelpers.InvokePrivate(field, "OnEnable");
        TestHelpers.SetPrivateField(field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset"));
        TestHelpers.SetPrivateField(field, "_updateGrass", AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute"));
        TestHelpers.SetPrivateField(field, "_lookMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat"));
        TestHelpers.SetPrivateField(field, "_ringMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat"));
        for (int i = 0; i < 3; i++)
        {
            GameObject stone = CreateCaptureObject("FixtureStone" + i);
            stone.transform.position = new Vector3((i - 1) * 2.2f, 0f, 1.6f);
            StoneTerrainClump clump = stone.AddComponent<StoneTerrainClump>();
            TestHelpers.SetPrivateField(clump, "_stoneMaterial", material);
            clump.Init((uint)(i + 3), 1.3f, null, null);
            clump.groundShadowEnabled = false;
            registry.Add(ZoneKind.Trample, stone.transform.position, 0.8f, 1f);
        }
        registry.Add(ZoneKind.Heal, new Vector3(-1.7f, 0f, -1.2f), 1.3f, 1f);
        registry.Add(ZoneKind.Hostile, new Vector3(1.7f, 0f, -0.9f), 1.2f, 0.85f);
        registry.PublishFrame(0.32f);
        RenderTexture target = Track(new RenderTexture(1440, 960, 24, RenderTextureFormat.ARGB32));
        target.Create();
        Texture2D texture = Track(new Texture2D(1440, 960, TextureFormat.RGB24, false));

        look.ApplyGlobals();
        field.UpdateField(registry);
        Render(camera, target, texture);
        int firstGrassPixels = GreenPixels(texture);
        Assert.Greater(firstGrassPixels, 20000, "First camera render contains the carpet.");

        // An Editor repaint can occur on another frame without a simulation LateUpdate.
        // Keep prepared buffers, advance the Editor once, then render the same camera again.
        yield return null;
        look.ApplyGlobals();
        Render(camera, target, texture);
        Assert.Greater(GreenPixels(texture), firstGrassPixels * 0.9f, "Repaint must resubmit prepared grass without another field LateUpdate.");
        byte[] bytes = texture.EncodeToPNG();
        Assert.Greater(bytes.Length, 10000);
        Assert.AreEqual(16384, field.bladeCount);
        string directory = "/Users/fc/Documents/healerlike-render-specs/captures";
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "render-ground-fixture.png"), bytes);

        field.SetZoneSnapshot(null, 0);
        yield return null;
        Render(camera, target, texture);
        Assert.Less(GreenPixels(texture), firstGrassPixels * 0.1f, "Revoking the borrowed zone snapshot must stop camera submissions.");
    }
}

}
