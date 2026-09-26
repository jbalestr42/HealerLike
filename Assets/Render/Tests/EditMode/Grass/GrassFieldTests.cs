using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
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
    // The keyword GrassBlade.mat carries so the look shader reads the tuft buffers
    static readonly string instancedKeyword = "HL_GRASS_INSTANCED";
    static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

    GameObject _go;
    GrassField _field;
    GraphicsBuffer _borrowedZones;

    static bool HasGraphicsDevice()
    {
        return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders
               && SystemInfo.supportsIndirectArgumentsBuffer;
    }

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("GrassFieldTest");
        _field = _go.AddComponent<GrassField>();
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
    }

    void SetAssets()
    {
        ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
        Material look = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
        Material ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");
        TestHelpers.SetPrivateField(_field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath));
        TestHelpers.SetPrivateField(_field, "_updateGrass", compute);
        TestHelpers.SetPrivateField(_field, "_lookMaterial", look);
        TestHelpers.SetPrivateField(_field, "_ringMaterial", ring);
    }

    // Built through the update a frame runs, with a camera to cull against
    void BuildOneCellField()
    {
        if (_borrowedZones == null)
        {
            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
        }

        Camera camera = _go.GetComponent<Camera>();
        if (camera == null)
        {
            camera = _go.AddComponent<Camera>();
        }

        _field.Init(oneCell, 1f, 0.5f, camera, _borrowedZones, ZonePacker.MaxZones);
        _field.tuftBudget = 65;
        SetAssets();
        _field.UpdateField(_borrowedZones, 0);
        Assert.IsTrue(_field.isReady);
    }

    // Every buffer the field owns: the argument buffers of the two tuft draws, then the seeds, the states and the
    // visible ids the compute writes
    List<GraphicsBuffer> OwnedBuffers()
    {
        List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
        buffers.Add(_field.tuftDraw.arguments);
        buffers.Add(_field.socleDraw.arguments);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (string name in new string[] { "_seeds", "_states", "_visibleTufts" })
        {
            GraphicsBuffer buffer = (GraphicsBuffer)typeof(GrassField).GetField(name, flags).GetValue(_field);
            Assert.IsTrue(buffer.IsValid(), name);
            buffers.Add(buffer);
        }
        return buffers;
    }

    // A field on the one-cell area stepped through a real zone registry, owning the ground when asked
    ZoneRegistry BuildWithRegistry(bool ownsGround)
    {
        Camera camera = _go.AddComponent<Camera>();
        ZoneRegistry registry = _go.AddComponent<ZoneRegistry>();
        registry.Init();
        _field.Init(oneCell, 1f, 0.5f, camera, registry.buffer, ZonePacker.MaxZones);
        _field.tuftBudget = 65;
        SetAssets();
        if (ownsGround)
        {
            TestHelpers.SetPrivateField(_field, "_groundShader",
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GroundSimulation.shader"));
        }

        registry.Add(ZoneKind.Trample, new Vector3(0f, 0.5f, 0f), 0.6f, 1f);
        registry.PublishFrame(0.2f);
        _field.UpdateField(registry);
        return registry;
    }

    [Test]
    public void UpdateField_GroundShader_OwnsAndPublishesTheGroundAroundTheField()
    {
        if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        BuildWithRegistry(true);

        Assert.IsNotNull(_field.ground);
        Assert.IsTrue(_field.ground.isValid);
        Assert.AreEqual(1, _field.ground.stampCount, "The trample zone became a stamp.");
        Rect area = _field.ground.volume.area;
        Assert.AreEqual(-3.5f, area.xMin, 1e-5f, "Three cells of margin around the one-cell field.");
        Assert.AreEqual(7f, area.width, 1e-5f);
        Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
        Assert.AreSame(_field.ground.motion, Shader.GetGlobalTexture(GroundSimulation.MotionId));

        TestHelpers.InvokePrivate(_field, "OnDisable");

        Assert.IsNull(_field.ground);
        Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId), "A released ground is unpublished.");
    }

    [Test]
    public void UpdateField_TrampleOverTime_FlattensTheTuftsUnderIt()
    {
        if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        ZoneRegistry registry = BuildWithRegistry(true);
        for (int frame = 1; frame < 30; frame++)
        {
            registry.PublishFrame(1f / 60f);
            _field.UpdateField(registry, 1f / 60f, frame / 60f);
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GraphicsBuffer seeds = (GraphicsBuffer)typeof(GrassField).GetField("_seeds", flags).GetValue(_field);
        GraphicsBuffer states = (GraphicsBuffer)typeof(GrassField).GetField("_states", flags).GetValue(_field);
        TuftSeed[] seedData = new TuftSeed[_field.tuftCount];
        TuftState[] stateData = new TuftState[_field.tuftCount];
        seeds.GetData(seedData);
        states.GetData(stateData);
        int flattened = 0;
        for (int i = 0; i < seedData.Length; i++)
        {
            Vector4 root = seedData[i].positionYaw;
            if (new Vector2(root.x, root.z).magnitude < 0.3f)
            {
                float height = seedData[i].heightWidthLean.x * stateData[i].leanHeightSpike.z;
                Assert.Less(height, 0.2f, $"Tuft {i} at {root} still stands {height} tall.");
                flattened++;
            }
        }

        Assert.Greater(flattened, 0);
    }

    [Test]
    public void UpdateField_NewLayout_RebuildsTheTuftsButKeepsTheGround()
    {
        if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        ZoneRegistry registry = BuildWithRegistry(true);
        GroundSimulation ground = _field.ground;

        _field.tuftBudget = 20;
        _field.UpdateField(registry, 1f / 60f, 1f);

        Assert.AreSame(ground, _field.ground, "Its motion and state live on.");
        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
    }

    [Test]
    public void UpdateField_NoGroundShader_OnlyReadsTheGround()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        GroundSimulation.Unpublish();
        BuildWithRegistry(false);

        Assert.IsNull(_field.ground);
        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
    }

    [Test]
    public void BladeSegments_OutsideRange_Clamps()
    {
        Assert.AreEqual(4, _field.bladeSegments);

        _field.bladeSegments = 0;
        Assert.AreEqual(1, _field.bladeSegments);

        _field.bladeSegments = 99;
        Assert.AreEqual(GrassBladeMesh.MaxSegments, _field.bladeSegments);
    }

    [Test]
    public void TuftBudget_Default_IsTheMaximum()
    {
        Assert.AreEqual(GrassLayout.MaxBudget, _field.tuftBudget);
    }

    [Test]
    public void TuftBudget_OutsideRange_Clamps()
    {
        _field.tuftBudget = int.MaxValue;

        Assert.AreEqual(GrassLayout.MaxBudget, _field.tuftBudget);

        _field.tuftBudget = -1;

        Assert.AreEqual(0, _field.tuftBudget);
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
        TestHelpers.WithLoggingDisabled(() => _field.SetZoneSnapshot(null, ZonePacker.MaxZones + 1));

        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void Init_WithoutZoneBuffer_LogsAndStaysUnbuilt()
    {
        string message = "[GrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.";
        LogAssert.Expect(LogType.Error, message);

        _field.Init(oneCell, 1f, 0.5f, null, null, ZonePacker.MaxZones);
        _field.UpdateField(null, 0);

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.tuftCount);
    }

    [Test]
    public void Init_NonFiniteCellSize_LogsAndStaysUnbuilt()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
        LogAssert.Expect(LogType.Error, new Regex(@"^\[GrassField\] Rejected area .* with cell size NaN"));

        _field.Init(oneCell, float.NaN, 0.5f, null, _borrowedZones, ZonePacker.MaxZones);

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void Release_CalledTwice_StaysReleased()
    {
        _field.Release();
        _field.Release();

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.tuftCount);
        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void UpdateField_WithCamera_BuildsFromTheInitArea()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        Camera camera = _go.AddComponent<Camera>();
        _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
        _field.Init(new Rect(2f, -3f, 4f, 2f), 1f, 0.5f, camera, _borrowedZones, ZonePacker.MaxZones);
        _field.tuftBudget = 80;
        SetAssets();

        _field.UpdateField(_borrowedZones, 3);

        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(72, _field.tuftCount); // 12 by 6 on the 4 by 2 area, the widest grid of at most 80
        Assert.AreEqual(3, _field.activeZoneCount);
    }

    [Test]
    public void Build_OnGraphicsDevice_DrawsTuftsAndSoclesWithTheLookShader()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        BuildOneCellField();

        Assert.IsTrue(_field.isReady);
        Assert.AreEqual(9, _field.tuftCount); // 3 by 3 at the wider spacing, below the budget of 65
        Assert.AreEqual("HL/Look/Primitive", _field.tuftDraw.material.shader.name);
        Assert.AreSame(_field.tuftDraw.material, _field.socleDraw.material);
        Assert.IsTrue(_field.tuftDraw.material.IsKeywordEnabled(instancedKeyword));
        uint[] data = new uint[5];
        _field.tuftDraw.arguments.GetData(data);
        Assert.AreEqual(GrassBladeMesh.Shared(_field.bladeSegments).GetIndexCount(0), data[0]); // four bent sides
        _field.socleDraw.arguments.GetData(data);
        Assert.AreEqual((uint)FacetedMeshes.SocleIndexCount, data[0]); // eight fan triangles
        Assert.AreEqual(ShadowCastingMode.On, _field.tuftDraw.shadowCastingMode);
        Assert.AreEqual(0f, _field.tuftDraw.properties.GetFloat("_HLGrassSpikeShadowsOnly"));
        Assert.AreEqual(ShadowCastingMode.Off, _field.socleDraw.shadowCastingMode);
        Assert.AreEqual(1f, _field.tuftDraw.properties.GetFloat("_HLTuftLean"));
        Assert.AreEqual(0f, _field.socleDraw.properties.GetFloat("_HLTuftLean"));
    }

    [Test]
    public void OnDisable_BuiltField_ReleasesItsBuffersAndKeepsBorrowedZonesAndMaterials()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        for (int cycle = 0; cycle < 3; cycle++)
        {
            BuildOneCellField();
            List<GraphicsBuffer> buffers = OwnedBuffers();
            Material tuftMaterial = _field.tuftDraw.material;

            // Runtime messages do not run on their own in EditMode
            TestHelpers.InvokePrivate(_field, "OnDisable");

            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsFalse(buffer.IsValid());
            }
            Assert.IsTrue(tuftMaterial != null);
            Assert.IsTrue(_borrowedZones.IsValid());
            Assert.IsFalse(_field.isReady);
            Assert.IsNull(_field.tuftDraw);
            Assert.IsNull(_field.socleDraw);
        }
    }

    [Test]
    public void OnDestroy_BuiltField_ReleasesItsBuffers()
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
}

}
