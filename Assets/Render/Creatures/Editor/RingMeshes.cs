using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Rings and discs for the primitive baker
    public static class RingMeshes
    {
        // Flat disc of radius one on the ground plane
        public static Mesh CreateDisc(int segments)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            return PrimitiveMeshBaker.CreateMesh("Disc", vertices, triangles);
        }

        // Thin ring of radius one, the uv carries the angle and the side across the band
        public static Mesh CreateAnnulus(int segments)
        {
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i % segments) * Mathf.PI * 2f / segments;
                for (int side = 0; side < 2; side++)
                {
                    float radius = 1f + (side - 0.5f) * 0.025f;
                    vertices[i * 2 + side] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    uv[i * 2 + side] = new Vector2(angle, side - 0.5f);
                }

                if (i == segments)
                {
                    continue;
                }

                int vertex = i * 2;
                int offset = i * 6;
                triangles[offset] = vertex;
                triangles[offset + 1] = vertex + 2;
                triangles[offset + 2] = vertex + 1;
                triangles[offset + 3] = vertex + 1;
                triangles[offset + 4] = vertex + 2;
                triangles[offset + 5] = vertex + 3;
            }

            return PrimitiveMeshBaker.CreateMesh("Annulus", vertices, triangles, null, uv);
        }
    }
}
