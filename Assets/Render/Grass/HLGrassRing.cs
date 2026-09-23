using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The heal ring built at runtime for the stage scene; the render manager path uses the baked annulus. Removed in D2.
    public static class HLGrassRing
    {
        public static Mesh CreateAnnulus(int segments = 128)
        {
            if (segments < 3 || segments > 16000)
            {
                Debug.LogError($"[HLGrassRing] Rejected {segments} segments, the ring needs 3 to 16000.");
                return null;
            }

            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] indices = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i % segments) * Mathf.PI * 2f / segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int side = 0; side < 2; side++)
                {
                    float radius = 1f + (side - 0.5f) * 0.025f;
                    vertices[i * 2 + side] = new Vector3(direction.x, 0f, direction.y) * radius;
                    uv[i * 2 + side] = new Vector2(angle, side - 0.5f);
                }

                if (i == segments)
                {
                    continue;
                }

                int vertex = i * 2;
                int triangle = i * 6;
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + 2;
                indices[triangle + 2] = vertex + 1;
                indices[triangle + 3] = vertex + 1;
                indices[triangle + 4] = vertex + 2;
                indices[triangle + 5] = vertex + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = "HLGrassHealRing";
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
