using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Grass
{

public class HLGrassFieldTests
{
    GameObject _go;
    HLGrassField _field;
    GraphicsBuffer _borrowedZones;

    static bool HasGraphicsDevice()
    {
        return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders && SystemInfo.supportsIndirectArgumentsBuffer;
    }

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("HLGrassFieldTest");
        _field = _go.AddComponent<HLGrassField>();
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
        }
    }

    void BuildOneCellField()
    {
        GridManager grid = _go.GetComponent<GridManager>();
        if (grid == null)
        {
            grid = _go.AddComponent<GridManager>();
            grid.width = 1;
            grid.height = 1;
            grid.size = 1f;
            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, HLGrassField.MaxZones, HLZone.Stride);
        }
        _field.Init(grid, _go.transform, null, _borrowedZones, HLGrassField.MaxZones);
        _field.bladeBudget = 65;
        TestHelpers.SetPrivateField(_field, "_updateGrass", AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute"));
        TestHelpers.SetPrivateField(_field, "_lookMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat"));
        TestHelpers.SetPrivateField(_field, "_ringShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"));

        MethodInfo build = typeof(HLGrassField).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);
        HLGrassBuildKey key = new HLGrassBuildKey(grid, 0.5f, 1, _field.bladeBudget);
        Assert.IsTrue((bool)build.Invoke(_field, new object[] { key }));
    }

    List<GraphicsBuffer> OwnedBuffers()
    {
        List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
        foreach (FieldInfo member in typeof(HLGrassField).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
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
        Assert.AreEqual(HLGrassLayout.DefaultBudget, _field.bladeBudget);
    }

    [Test]
    public void BladeBudget_OutsideRange_Clamps()
    {
        _field.bladeBudget = int.MaxValue;
        Assert.AreEqual(HLGrassLayout.MaxBudget, _field.bladeBudget);

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
        TestHelpers.WithLoggingDisabled(() => _field.SetZoneCount(HLGrassField.MaxZones + 1));

        Assert.AreEqual(0, _field.activeZoneCount);
    }

    [Test]
    public void Init_WithoutZoneBuffer_StaysUnbuilt()
    {
        TestHelpers.WithLoggingDisabled(() => _field.Init(null, null, null, null, HLGrassField.MaxZones));
        TestHelpers.InvokePrivate(_field, "LateUpdate");

        Assert.IsFalse(_field.isReady);
        Assert.AreEqual(0, _field.bladeCount);
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
        Assert.IsTrue(_field.bladeDraw.material.IsKeywordEnabled(HLGrassPalette.InstancedKeyword));
        uint[] data = new uint[5];
        _field.bladeDraw.arguments.GetData(data);
        Assert.AreEqual(9u * (uint)HLGrassField.BladeSides, data[0]); // one cone: side quads and base cap
        Assert.AreEqual(5, OwnedBuffers().Count); // seeds, states, visible ids, blade and ring arguments
    }

    [Test]
    public void OnDisable_BuiltField_ReleasesOwnedResourcesAndKeepsBorrowedZones()
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
            Assert.IsTrue(bladeMaterial == null);
            Assert.IsTrue(ringMaterial == null);
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
