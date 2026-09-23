using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Flat-shaded and planar shapes for the primitive baker
    public static class FacetedMeshes
    {
        // The stones' flat-shaded four-sided cone, base on the ground
        public static Mesh CreatePyramid()
        {
            Vector3[] points =
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                Vector3.up
            };
            int[] faces = { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0, 0, 1, 2, 0, 2, 3 };
            Vector3[] vertices = new Vector3[18];
            Vector3[] normals = new Vector3[18];
            int[] triangles = new int[18];
            for (int i = 0; i < 18; i += 3)
            {
                Vector3 origin = points[faces[i]];
                Vector3 normal = Vector3.Cross(points[faces[i + 1]] - origin, points[faces[i + 2]] - origin).normalized;
                for (int j = 0; j < 3; j++)
                {
                    vertices[i + j] = points[faces[i + j]];
                    normals[i + j] = normal;
                    triangles[i + j] = i + j;
                }
            }

            Mesh mesh = new Mesh { name = "Pyramid" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        // Two-sided planar fan: eight long rays alternating with short notches
        public static Mesh CreateStar()
        {
            Vector3[] vertices = new Vector3[34];
            int[] triangles = new int[96];
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                float radius = i % 2 == 0 ? 1f : 0.32f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                vertices[i + 18] = vertices[i + 1];

                int next = (i + 1) % 16 + 1;
                int offset = i * 6;
                triangles[offset] = 0;
                triangles[offset + 1] = i + 1;
                triangles[offset + 2] = next;
                triangles[offset + 3] = 17;
                triangles[offset + 4] = next + 17;
                triangles[offset + 5] = i + 18;
            }

            return PrimitiveMeshBaker.CreateMesh("Star", vertices, triangles);
        }

        // Flat-shaded octahedron, kept asymmetric on purpose
        public static Mesh CreateBoulder()
        {
            Vector3[] vertices = new Vector3[24];
            int[] triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                float b = (i + 1) * Mathf.PI * 0.5f;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 q = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
                int k = i * 6;
                vertices[k] = p;
                vertices[k + 1] = new Vector3(0.15f, 1f, 0f);
                vertices[k + 2] = q;
                vertices[k + 3] = q;
                vertices[k + 4] = new Vector3(-0.1f, -0.7f, 0.1f);
                vertices[k + 5] = p;
                for (int j = 0; j < 6; j++)
                {
                    triangles[k + j] = k + j;
                }
            }

            return PrimitiveMeshBaker.CreateMesh("Boulder", vertices, triangles);
        }
    }
}
