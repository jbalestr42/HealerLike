using System;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public static class HLGrassRing
    {
        public static Mesh CreateAnnulus(int segments = 128)
        {
            if (segments < 3 || segments > 16000) throw new ArgumentOutOfRangeException(nameof(segments));
            var vertices = new Vector3[(segments + 1) * 2]; var uv = new Vector2[vertices.Length];
            var indices = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float a = (i % segments) * Mathf.PI * 2 / segments;
                for (int side = 0; side < 2; side++)
                {
                    var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    vertices[i * 2 + side] = new Vector3(direction.x, 0, direction.y) * (1 + (side - 0.5f) * 0.025f);
                    uv[i * 2 + side] = new Vector2(a, side - 0.5f);
                }
                if (i == segments) continue;
                int v = i * 2, t = i * 6;
                indices[t] = v; indices[t + 1] = v + 2; indices[t + 2] = v + 1;
                indices[t + 3] = v + 1; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "HLGrassHealRing", vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
