using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stage
{

public class RenderAssetsTests
{
    GameObject _host;
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("RenderAssetsTests");
        _mesh = new Mesh();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_host);
        Object.DestroyImmediate(_mesh);
    }

    [Test]
    public void Load_ShippedAsset_ReturnsIt()
    {
        Material material = RenderAssets.Load<Material>(EnvironmentAuthoring.PlantMaterialPath);

        Assert.IsNotNull(material);
    }

    [Test]
    public void Load_MissingPath_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "[RenderAssets] Missing Assets/Render/Nothing.mat.");

        Material material = RenderAssets.Load<Material>("Assets/Render/Nothing.mat");

        Assert.IsNull(material);
    }

    [Test]
    public void SetReference_SerializedField_WritesTheReference()
    {
        MeshFilter filter = _host.AddComponent<MeshFilter>();

        RenderAssets.SetReference(filter, "m_Mesh", _mesh);

        Assert.AreSame(_mesh, filter.sharedMesh);
    }

    [Test]
    public void SetReference_UnknownField_LogsAndLeavesTheTarget()
    {
        MeshFilter filter = _host.AddComponent<MeshFilter>();
        LogAssert.Expect(LogType.Error, "[RenderAssets] MeshFilter has no field _nothing.");

        RenderAssets.SetReference(filter, "_nothing", _mesh);

        Assert.IsNull(filter.sharedMesh);
    }
}

}
