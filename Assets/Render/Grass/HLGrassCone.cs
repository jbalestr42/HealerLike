using UnityEngine;
namespace HealerLike.Render.Grass
{
    public static class HLGrassCone
    {
        public static Mesh CreateCone()
        {
            var vertices = new Vector3[12]; var uv = new Vector2[12]; var indices = new int[12];
            for (int face = 0; face < 4; face++)
            {
                float a = face * Mathf.PI / 2, b = (face + 1) * Mathf.PI / 2;
                vertices[face * 3] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[face * 3 + 1] = Vector3.up;
                vertices[face * 3 + 2] = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                uv[face * 3 + 1] = Vector2.up;
            }
            for (int i = 0; i < 12; i++) indices[i] = i;
            var mesh = new Mesh { name = "HLGrassCone", vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
