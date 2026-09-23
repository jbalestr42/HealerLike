using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class PrimitiveMeshesTests
    {
        public static PrimitiveMeshes Meshes()
        {
            return AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
        }

        [TestCase(Primitive.Sphere)]
        [TestCase(Primitive.Capsule)]
        [TestCase(Primitive.Cone)]
        [TestCase(Primitive.Torus)]
        [TestCase(Primitive.CylinderSegment)]
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

        [Test]
        public void Dispose_LiveRigs_LeaveSharedMeshesAlive()
        {
            GameObject parent = new GameObject("HLMeshOwner");
            CreatureRecipe recipe = CreatureValidatorTests.Recipe();
            Material material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLLook.shader"));
            CreatureRig rig = new CreatureRig();
            rig.Init(recipe, parent.transform, material, Meshes());
            Mesh mesh = rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;

            rig.Dispose();

            Assert.IsTrue(mesh);
            Assert.IsTrue(AssetDatabase.Contains(mesh));
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(material);
        }
    }
}
