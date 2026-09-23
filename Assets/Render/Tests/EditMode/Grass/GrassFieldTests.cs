using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{

public class GrassFieldTests
{
    static readonly Rect oneCell = new Rect(-0.5f, -0.5f, 1f, 1f);

    GameObject _go;
    GrassField _field;
    GraphicsBuffer _borrowedZones;

    static bool HasGraphicsDevice()
    {
        return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders && SystemInfo.supportsIndirectArgumentsBuffer;
    }

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("HLGrassFieldTest");
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
        LogAssert.Expect(LogType.Error, "[HLGrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.");

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
        LogAssert.Expect(LogType.Error, new Regex(@"^\[HLGrassField\] Rejected area .* with cell size NaN"));

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
}
}
