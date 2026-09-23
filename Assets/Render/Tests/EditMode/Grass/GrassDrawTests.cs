using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{

public class GrassDrawTests
{
    Mesh _mesh;
    Material _material;
    GrassDraw _draw;

    [SetUp]
    public void SetUp()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            Assert.Ignore("Indirect argument buffers need a graphics device; run with -force-metal.");
        }
        _mesh = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset").tuft;
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader"));
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
    }

    [Test]
    public void Constructor_Mesh_WritesIndexAndInstanceCounts()
    {
        uint[] data = new uint[5];

        _draw.arguments.GetData(data);

        Assert.AreEqual(_mesh.GetIndexCount(0), data[0]);
        Assert.AreEqual(7u, data[1]);
        Assert.AreEqual((uint)GrassTuft.IndexCount, data[0]); // faceted sides and base cap
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
}

}
