using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLPrimitiveMeshesTests
    {
        [TestCase(HLPrimitive.Sphere)] [TestCase(HLPrimitive.Capsule)] [TestCase(HLPrimitive.Cone)]
        [TestCase(HLPrimitive.Torus)] [TestCase(HLPrimitive.CylinderSegment)]
        public void GeometryIsFiniteNormalizedBoundedAndDeterministic(HLPrimitive type)
        {
            var mesh = HLPrimitiveMeshes.Get(type, 12, 6);
            var vertices = mesh.vertices; var normals = mesh.normals; var indices = mesh.triangles;
            Assert.Greater(indices.Length, 0); Assert.AreEqual(vertices.Length, normals.Length);
            foreach (var i in indices) Assert.That(i, Is.InRange(0, vertices.Length - 1));
            foreach (var v in vertices) { Assert.IsTrue(HLChainSolver.Finite(v)); Assert.LessOrEqual(Mathf.Abs(v.x), .50001f); Assert.LessOrEqual(Mathf.Abs(v.y), .50001f); Assert.LessOrEqual(Mathf.Abs(v.z), .50001f); }
            foreach (var n in normals) Assert.That(n.magnitude, Is.EqualTo(1).Within(1e-5));
            Assert.That(mesh.bounds.size.x, Is.EqualTo(1).Within(1e-5));
            Assert.That(mesh.bounds.size.y, Is.EqualTo(type == HLPrimitive.Torus ? Mathf.Sqrt(3) * .1f : 1).Within(1e-5));
            HLPrimitiveMeshes.ReleaseAll(); var again = HLPrimitiveMeshes.Get(type, 12, 6);
            CollectionAssert.AreEqual(vertices, again.vertices); CollectionAssert.AreEqual(indices, again.triangles);
        }
        [TearDown] public void Cleanup() => HLPrimitiveMeshes.ReleaseAll();
    }
}
