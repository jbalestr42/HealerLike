using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassConeTests
    {
        [Test] public void ConeHasFourOutwardFlatFacesWithoutBottomCap()
        {
            var mesh = HLGrassCone.CreateCone();
            try
            {
                Assert.AreEqual(12, mesh.vertexCount); Assert.AreEqual(12, mesh.triangles.Length);
                var v = mesh.vertices; var n = mesh.normals;
                for (int face = 0; face < 4; face++)
                {
                    int i = face * 3; Assert.AreEqual(Vector3.up, v[i + 1]);
                    Assert.AreEqual(0, v[i].y); Assert.AreEqual(0, v[i + 2].y);
                    Assert.Greater(Vector3.Dot(n[i], v[i] + v[i + 2]), 0);
                    Assert.That(n[i].magnitude, Is.EqualTo(1).Within(0.00001));
                    Assert.AreEqual(n[i], n[i + 1]); Assert.AreEqual(n[i], n[i + 2]);
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
