using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassBladeTests
    {
        [Test] public void StripHasFourTaperedRowsAndSixValidTriangles()
        {
            var mesh = HLGrassBlade.CreateStrip();
            try
            {
                Assert.AreEqual(8, mesh.vertexCount); Assert.AreEqual(18, mesh.triangles.Length);
                var v = mesh.vertices; var uv = mesh.uv; var indices = mesh.triangles;
                Assert.AreEqual(Vector3.zero, (v[0] + v[1]) / 2);
                for (int row = 0; row < 4; row++)
                {
                    Assert.AreEqual(row / 3f, v[row * 2].y); Assert.AreEqual(row / 3f, uv[row * 2].y);
                    Assert.AreEqual(0, uv[row * 2].x); Assert.AreEqual(1, uv[row * 2 + 1].x);
                    if (row > 1) Assert.Less(v[row * 2 + 1].x, v[row * 2 - 1].x);
                    if (row == 1) Assert.Greater(v[3].x, v[1].x);
                }
                for (int i = 0; i < indices.Length; i += 3)
                    Assert.Greater(Vector3.Cross(v[indices[i + 1]] - v[indices[i]], v[indices[i + 2]] - v[indices[i]]).sqrMagnitude, 0);
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
