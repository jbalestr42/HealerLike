using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using HealerLike.Render.Look;
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

    // The argument buffers of the two tuft draws, the ones a caller can reach
    List<GraphicsBuffer> DrawBuffers()
    {
        List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
        buffers.Add(_field.tuftDraw.arguments);
        buffers.Add(_field.socleDraw.arguments);
        return buffers;
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
        Assert.AreEqual(64, _field.tuftCount); // 8 by 8, the widest grid of at most 65
        Assert.AreEqual("HL/Look/Primitive", _field.tuftDraw.material.shader.name);
        Assert.AreSame(_field.tuftDraw.material, _field.socleDraw.material);
        Assert.IsTrue(_field.tuftDraw.material.IsKeywordEnabled(instancedKeyword));
        uint[] data = new uint[5];
        _field.tuftDraw.arguments.GetData(data);
        Assert.AreEqual((uint)FacetedMeshes.TuftIndexCount, data[0]); // four sides
        _field.socleDraw.arguments.GetData(data);
        Assert.AreEqual((uint)FacetedMeshes.SocleIndexCount, data[0]); // eight fan triangles
        Assert.AreEqual(ShadowCastingMode.On, _field.tuftDraw.shadowCastingMode);
        Assert.AreEqual(ShadowCastingMode.Off, _field.socleDraw.shadowCastingMode);
        Assert.AreEqual(1f, _field.tuftDraw.properties.GetFloat("_HLTuftLean"));
        Assert.AreEqual(0f, _field.socleDraw.properties.GetFloat("_HLTuftLean"));
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
    public void OnDisable_BuiltField_ReleasesItsDrawsAndKeepsBorrowedZonesAndMaterials()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }

        for (int cycle = 0; cycle < 3; cycle++)
        {
            BuildOneCellField();
            List<GraphicsBuffer> buffers = DrawBuffers();
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
    public void OnDestroy_BuiltField_ReleasesItsDraws()
    {
        if (!HasGraphicsDevice())
        {
            Assert.Ignore("Requires a graphics device; run with -force-metal.");
        }
        BuildOneCellField();
        List<GraphicsBuffer> buffers = DrawBuffers();

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
