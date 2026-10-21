using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The tuft as a four-sided spire open at its base, the shape of FacetedMeshes.CreatePyramid, cut into
    // segments along its height so it can bend. Each side keeps one flat normal, so an unbent spire shades
    // exactly like the pyramid. One segment is the pyramid itself.
    public static class GrassBladeMesh
    {
        public static readonly int MaxSegments = 8;

        static readonly Vector3[] corners =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0.5f)
        };

        static readonly Dictionary<int, Mesh> shared = new Dictionary<int, Mesh>();

        // One mesh per segment count for the life of the domain
        public static Mesh Shared(int segments)
        {
            segments = Mathf.Clamp(segments, 1, MaxSegments);
            if (!shared.TryGetValue(segments, out Mesh mesh) || mesh == null)
            {
                mesh = Create(segments);
                mesh.hideFlags = HideFlags.HideAndDontSave;
                shared[segments] = mesh;
            }

            return mesh;
        }

        public static Mesh Create(int segments)
        {
            segments = Mathf.Clamp(segments, 1, MaxSegments);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int side = 0; side < corners.Length; side++)
            {
                Vector3 a = corners[side];
                Vector3 b = corners[(side + 1) % corners.Length];
                // The winding of the pyramid's (corner, apex, next corner) facet
                Vector3 normal = Vector3.Cross(Vector3.up - a, b - a).normalized;
                int first = vertices.Count;
                for (int ring = 0; ring < segments; ring++)
                {
                    float t = (float)ring / segments;
                    vertices.Add(Ring(a, t));
                    vertices.Add(Ring(b, t));
                }

                vertices.Add(Vector3.up);
                int apex = vertices.Count - 1;
                for (int i = 0; i < vertices.Count - first; i++)
                {
                    normals.Add(normal);
                }

                for (int ring = 0; ring < segments; ring++)
                {
                    int lowA = first + ring * 2;
                    int lowB = lowA + 1;
                    if (ring == segments - 1)
                    {
                        triangles.Add(lowA);
                        triangles.Add(apex);
                        triangles.Add(lowB);
                        continue;
                    }

                    int highA = lowA + 2;
                    int highB = lowA + 3;
                    triangles.Add(lowA);
                    triangles.Add(highA);
                    triangles.Add(lowB);
                    triangles.Add(lowB);
                    triangles.Add(highA);
                    triangles.Add(highB);
                }
            }

            Mesh mesh = new Mesh { name = "GrassBlade" + segments };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A corner of the base drawn in toward the axis as the spire rises
        static Vector3 Ring(Vector3 corner, float t)
        {
            return new Vector3(corner.x * (1f - t), t, corner.z * (1f - t));
        }
    }
}
