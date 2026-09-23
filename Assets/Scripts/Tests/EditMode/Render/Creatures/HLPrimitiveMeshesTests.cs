using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLPrimitiveMeshesTests
    {
        [TearDown]
        public void Cleanup()
        {
            HLPrimitiveMeshes.ReleaseAll();
        }

        [TestCase(HLPrimitive.Sphere)]
        [TestCase(HLPrimitive.Capsule)]
        [TestCase(HLPrimitive.Cone)]
        [TestCase(HLPrimitive.Torus)]
        [TestCase(HLPrimitive.CylinderSegment)]
        public void GeometryIsFiniteNormalizedBoundedAndDeterministic(HLPrimitive type)
        {
            Mesh mesh = HLPrimitiveMeshes.Get(type, 12, 6);
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
                Assert.IsTrue(HLChainSolver.Finite(vertex));
                Assert.LessOrEqual(Mathf.Abs(vertex.x), 0.50001f);
                Assert.LessOrEqual(Mathf.Abs(vertex.y), 0.50001f);
                Assert.LessOrEqual(Mathf.Abs(vertex.z), 0.50001f);
            }

            foreach (Vector3 normal in normals)
            {
                Assert.That(normal.magnitude, Is.EqualTo(1).Within(0.00001));
            }

            Assert.That(mesh.bounds.size.x, Is.EqualTo(1).Within(0.00001));
            float height = type == HLPrimitive.Torus ? Mathf.Sqrt(3f) * 0.1f : 1f;
            Assert.That(mesh.bounds.size.y, Is.EqualTo(height).Within(0.00001));
            HLPrimitiveMeshes.ReleaseAll();
            Mesh again = HLPrimitiveMeshes.Get(type, 12, 6);
            CollectionAssert.AreEqual(vertices, again.vertices);
            CollectionAssert.AreEqual(indices, again.triangles);
        }

        [Test]
        public void ReleaseAllCannotInvalidateMeshesUsedByLiveRig()
        {
            GameObject parent = new GameObject("HLMeshOwner");
            HLCreatureRecipe recipe = HLCreatureValidatorTests.Recipe();
            Material material = new Material(Shader.Find("HL/Look/Primitive"));
            HLCreatureRig rig = HLCreatureRig.Build(recipe, parent.transform, material);
            Mesh mesh = rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;
            try
            {
                HLPrimitiveMeshes.ReleaseAll();
                Assert.IsTrue(mesh);
            }
            finally
            {
                rig.Dispose();
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(material);
            }

            Assert.IsFalse(mesh);
        }
    }
}
