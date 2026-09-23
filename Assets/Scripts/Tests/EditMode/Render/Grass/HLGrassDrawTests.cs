using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Grass
{

public class HLGrassDrawTests
{
    Mesh _mesh;
    Material _material;
    HLGrassDraw _draw;

    [SetUp]
    public void SetUp()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            Assert.Ignore("Indirect argument buffers need a graphics device; run with -force-metal.");
        }
        _mesh = HLPrimitiveMeshes.Get(HLPrimitive.Cone, HLGrassField.BladeSides, 2);
        _material = new Material(Shader.Find("HL/Look/Primitive"));
        _draw = new HLGrassDraw(_mesh, _material, 7, new Bounds(Vector3.zero, Vector3.one), 3);
    }

    [TearDown]
    public void TearDown()
    {
        if (_draw != null)
        {
            _draw.Release();
        }
        HLPrimitiveMeshes.ReleaseAll();
    }

    [Test]
    public void Constructor_Mesh_WritesIndexAndInstanceCounts()
    {
        uint[] data = new uint[5];

        _draw.arguments.GetData(data);

        Assert.AreEqual(_mesh.GetIndexCount(0), data[0]);
        Assert.AreEqual(7u, data[1]);
        Assert.AreEqual(9u * (uint)HLGrassField.BladeSides, data[0]); // side quads and base cap
    }

    [Test]
    public void Constructor_Material_KeepsMaterialAndCreatesProperties()
    {
        Assert.AreSame(_material, _draw.material);
        Assert.IsNotNull(_draw.properties);
    }

    [Test]
    public void Release_OwnedResources_DisposesArgumentsAndMaterialButNotMesh()
    {
        GraphicsBuffer arguments = _draw.arguments;

        _draw.Release();

        Assert.IsFalse(arguments.IsValid());
        Assert.IsTrue(_material == null);
        Assert.IsTrue(_mesh != null);
        Assert.IsNull(_draw.arguments);
    }
}
}
