using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Flat-shaded shapes for the primitive baker
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

        // Solid eight-point star: the rim is a thin band, a low apex sits on each face
        public static Mesh CreateStar()
        {
            int points = 16;
            float band = 0.06f;
            float apex = 0.3f;
            Vector3 front = new Vector3(0f, 0f, -apex);
            Vector3 back = new Vector3(0f, 0f, apex);
            List<Vector3> corners = new List<Vector3>();
            for (int i = 0; i < points; i++)
            {
                Vector3 a = StarRim(i);
                Vector3 b = StarRim(i + 1);
                Vector3 frontA = a + Vector3.back * band;
                Vector3 frontB = b + Vector3.back * band;
                Vector3 backA = a + Vector3.forward * band;
                Vector3 backB = b + Vector3.forward * band;
                corners.AddRange(new Vector3[] { front, frontA, frontB });
                corners.AddRange(new Vector3[] { back, backA, backB });
                corners.AddRange(new Vector3[] { frontA, backA, backB });
                corners.AddRange(new Vector3[] { frontA, backB, frontB });
            }

            return CreateFlatShaded("Star", corners);
        }

        // Eight long rays alternating with short notches, in the XY plane
        static Vector3 StarRim(int index)
        {
            float angle = index * Mathf.PI / 8f;
            float radius = index % 2 == 0 ? 1f : 0.32f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        // One normal per triangle. The shapes are star-shaped around the origin, so a triangle whose normal
        // points at the origin is turned around.
        static Mesh CreateFlatShaded(string name, List<Vector3> corners)
        {
            Vector3[] vertices = new Vector3[corners.Count];
            Vector3[] normals = new Vector3[corners.Count];
            int[] triangles = new int[corners.Count];
            for (int i = 0; i < corners.Count; i += 3)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                Vector3 c = corners[i + 2];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, a + b + c) < 0f)
                {
                    Vector3 swap = b;
                    b = c;
                    c = swap;
                    normal = -normal;
                }

                vertices[i] = a;
                vertices[i + 1] = b;
                vertices[i + 2] = c;
                for (int j = 0; j < 3; j++)
                {
                    normals[i + j] = normal;
                    triangles[i + j] = i + j;
                }
            }

            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
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
