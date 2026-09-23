using System;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassRingTests
    {
        [Test] public void RingHasClosedSeamAndUpwardWinding()
        {
            var mesh = HLGrassRing.CreateAnnulus();
            try
            {
                Assert.AreEqual(258, mesh.vertexCount); Assert.AreEqual(768, mesh.triangles.Length);
                var v = mesh.vertices; var indices = mesh.triangles;
                Assert.AreEqual(v[0], v[256]); Assert.AreEqual(v[1], v[257]);
                for (int i = 0; i < indices.Length; i += 3)
                    Assert.Greater(Vector3.Cross(v[indices[i + 1]] - v[indices[i]], v[indices[i + 2]] - v[indices[i]]).y, 0);
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        [Test] public void RejectsTooFewSegments() => Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassRing.CreateAnnulus(2));
    }
}
