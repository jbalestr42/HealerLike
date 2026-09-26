using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class BodyMeshesTests
{
    GameObject _root;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("body");
        Part("Foot", new Vector3(0f, 0.3f, 0f));
        Part("Crown", new Vector3(0f, 5f, 0f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    GameObject Part(string name, Vector3 position)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(_root.transform, false);
        part.transform.localPosition = position;
        return part;
    }

    [Test]
    public void Append_Ceiling_KeepsOnlyTheMeshesThatReachTheGrass()
    {
        BodyMeshes meshes = new BodyMeshes();
        meshes.Refresh(_root.GetComponentsInChildren<MeshFilter>());
        BodyCapsule[] into = new BodyCapsule[4];

        Assert.AreEqual(2, meshes.count);
        Assert.AreEqual(1, meshes.Append(into, 0, 1.5f, 8));
        Assert.AreEqual(new Vector3(0f, 0.3f, 0f), into[0].start);
        Assert.AreEqual(2, meshes.Append(into, 0, 10f, 8));
    }

    [Test]
    public void Append_LimitRoomOrHiddenParts_WritesLess()
    {
        BodyMeshes meshes = new BodyMeshes();
        meshes.Refresh(_root.GetComponentsInChildren<MeshFilter>());
        BodyCapsule[] into = new BodyCapsule[4];

        Assert.AreEqual(1, meshes.Append(into, 0, 10f, 1));
        Assert.AreEqual(1, meshes.Append(into, 3, 10f, 8));
        Assert.AreEqual(0, meshes.Append(null, 0, 10f, 8));
        _root.transform.Find("Foot").gameObject.SetActive(false);
        Assert.AreEqual(1, meshes.Append(into, 0, 10f, 8), "An inactive part presses nothing.");
    }

    [Test]
    public void Refresh_NoRig_Empties()
    {
        BodyMeshes meshes = new BodyMeshes();
        meshes.Refresh(_root.GetComponentsInChildren<MeshFilter>());

        meshes.Refresh((HealerLike.Render.Creatures.CreatureRig)null);

        Assert.AreEqual(0, meshes.count);
    }
}

}
