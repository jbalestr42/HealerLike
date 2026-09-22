using UnityEngine;
namespace HealerLike.Render.Grass
{
    public static class HLGrassBlade
    {
        public static Mesh CreateStrip()
        {
            var vertices = new Vector3[8]; var uv = new Vector2[8]; var indices = new int[18];
            float[] widths = { 0.55f, 1f, 0.65f, 0.015f };
            for (int row = 0; row < 4; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    vertices[row * 2 + side] = new Vector3((side - 0.5f) * widths[row], row / 3f, 0);
                    uv[row * 2 + side] = new Vector2(side, row / 3f);
                }
                if (row == 3) continue;
                int i = row * 6, v = row * 2;
                indices[i] = v; indices[i + 1] = v + 1; indices[i + 2] = v + 2;
                indices[i + 3] = v + 1; indices[i + 4] = v + 3; indices[i + 5] = v + 2;
            }
            var mesh = new Mesh { name = "HLGrassBlade", vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
