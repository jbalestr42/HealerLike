using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Stones;

namespace HealerLike.Render.Creatures
{

public class PrimitiveMeshesTests
{
    GameObject _parent;
    CreatureRecipe _recipe;
    Material _material;

    public static PrimitiveMeshes Meshes()
    {
        return AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
    }

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("MeshOwner");
        _recipe = CreatureValidatorTests.Recipe();
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader"));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_parent);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [TestCase(Primitive.Sphere)]
    [TestCase(Primitive.Capsule)]
    [TestCase(Primitive.Cone)]
    [TestCase(Primitive.Torus)]
    [TestCase(Primitive.CylinderSegment)]
    [TestCase(Primitive.Leaf)]
    public void GetMesh_BakedPrimitive_IsFiniteNormalizedAndBounded(Primitive type)
    {
        Mesh mesh = Meshes().GetMesh(type);

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] indices = mesh.triangles;
        Assert.Greater(indices.Length, 0);
        Assert.AreEqual(vertices.Length, normals.Length);
        foreach (int index in indices)
        {
            Assert.That(index, Is.InRange(0, vertices.Length - 1));
        }

        foreach (Vector3 vertex in vertices)
        {
            Assert.IsTrue(float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z));
            Assert.LessOrEqual(Mathf.Abs(vertex.x), 0.50001f);
            Assert.LessOrEqual(Mathf.Abs(vertex.y), 0.50001f);
            Assert.LessOrEqual(Mathf.Abs(vertex.z), 0.50001f);
        }

        foreach (Vector3 normal in normals)
        {
            Assert.That(normal.magnitude, Is.EqualTo(1).Within(0.00001));
        }

        Assert.That(mesh.bounds.size.x, Is.EqualTo(1).Within(0.00001));
        float height = type == Primitive.Torus ? Mathf.Sqrt(3f) / 12f : 1f; // tube ratio 0.2, six rings
        Assert.That(mesh.bounds.size.y, Is.EqualTo(height).Within(0.00001));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(5, 1)] // wraps round the two variants
    public void GetMesh_StoneVariant_PicksByIndex(int variant, int expected)
    {
        PrimitiveMeshes meshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
        StoneVariants variants = ScriptableObject.CreateInstance<StoneVariants>();
        Mesh first = new Mesh();
        Mesh second = new Mesh();
        variants.meshes = new Mesh[] { first, second };
        meshes.stoneVariants = variants;

        Mesh mesh = meshes.GetMesh(Primitive.Stone, variant);

        Assert.AreEqual(variants.meshes[expected], mesh);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        Object.DestroyImmediate(variants);
        Object.DestroyImmediate(meshes);
    }

    [Test]
    public void GetMesh_OtherPrimitiveWithVariant_IgnoresTheVariant()
    {
        Assert.AreEqual(Meshes().cone, Meshes().GetMesh(Primitive.Cone, 4));
    }

    [Test]
    public void Dispose_LiveRig_LeavesSharedMeshesAlive()
    {
        CreatureRig rig = new CreatureRig();
        rig.Init(_recipe, _parent.transform, _material, Meshes());
        Mesh mesh = rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;

        rig.Dispose();

        Assert.IsTrue(mesh);
        Assert.IsTrue(AssetDatabase.Contains(mesh));
    }
}

}
