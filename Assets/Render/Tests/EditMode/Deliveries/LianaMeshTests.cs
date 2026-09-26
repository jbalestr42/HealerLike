using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{

public class LianaMeshTests
{
    GameObject _parent;
    LianaMesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("ArmOwner");
        _mesh = new LianaMesh();
        _mesh.Init(_parent.transform, RenderTestAssets.LoadLookMaterial(), Color.white, 3);
    }

    [TearDown]
    public void TearDown()
    {
        _mesh.Dispose();
        Object.DestroyImmediate(_parent);
    }

    [Test]
    public void Dispose_ParentDestroyedFirst_StillReleasesTheOwnedMesh()
    {
        Mesh mesh = _mesh.container.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(_parent);

        _mesh.Dispose();
        _mesh.Dispose();

        Assert.IsTrue(mesh == null);
    }
}

}
