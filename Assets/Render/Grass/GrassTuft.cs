using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // One grass tuft: three fused four-sided pyramids, each closed by its base cap.
    // x and z are in tuft widths and y in tuft heights, the main body spans -0.5..0.5 and 0..1.
    // Place mirrors HLPlaceGrassTuft in GrassInstancing.hlsl, a rigid transform with no bending.
    public static class GrassTuft
    {
        public static readonly int BodyCount = 3;
        public static readonly int IndicesPerBody = 18;

        // Offset x, z, sink y, then width, height, tilt and spin in degrees, one row per body
        static readonly float[,] bodies =
        {
            { 0f, -0.08f, 0f, 1f, 1f, 0f, 0f },
            { 0.3f, 0.16f, -0.07f, 0.72f, 0.78f, 14f, 35f },
            { -0.3f, 0.14f, -0.07f, 0.62f, 0.64f, 17f, 20f }
        };

        public static Mesh CreateMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < BodyCount; i++)
            {
                Vector3 offset = new Vector3(bodies[i, 0], bodies[i, 2], bodies[i, 1]);
                Vector2 outward = new Vector2(offset.x, offset.z).normalized;
                Vector2 tilt = outward * (bodies[i, 5] * Mathf.Deg2Rad);
                Quaternion rotation = Quaternion.AngleAxis(bodies[i, 6], Vector3.up);
                AddPyramid(vertices, normals, offset, bodies[i, 3], bodies[i, 4], rotation, tilt);
            }

            int[] triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            Mesh mesh = new Mesh { name = "Tuft" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Flat-shaded pyramid, sides then a two-triangle base cap, the winding of FacetedMeshes.CreatePyramid
        static void AddPyramid(List<Vector3> vertices, List<Vector3> normals, Vector3 offset, float width, float height, Quaternion spin, Vector2 tilt)
        {
            Vector3[] points =
            {
                new Vector3(-0.5f * width, 0f, -0.5f * width),
                new Vector3(0.5f * width, 0f, -0.5f * width),
                new Vector3(0.5f * width, 0f, 0.5f * width),
                new Vector3(-0.5f * width, 0f, 0.5f * width),
                new Vector3(0f, height, 0f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Tilt(spin * points[i], tilt) + offset;
            }

            int[] faces = { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0, 0, 1, 2, 0, 2, 3 };
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 origin = points[faces[i]];
                Vector3 normal = Vector3.Cross(points[faces[i + 1]] - origin, points[faces[i + 2]] - origin).normalized;
                for (int j = 0; j < 3; j++)
                {
                    vertices.Add(points[faces[i + j]]);
                    normals.Add(normal);
                }
            }
        }

        // Rotates v about the horizontal axis that tips +Y toward lean, by the length of lean in radians
        public static Vector3 Tilt(Vector3 v, Vector2 lean)
        {
            float angle = Mathf.Max(lean.magnitude, 0.00001f);
            Vector3 axis = new Vector3(lean.y, 0f, -lean.x) / angle;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return v * cos + Vector3.Cross(axis, v) * sin + axis * (Vector3.Dot(axis, v) * (1f - cos));
        }

        // Scale by width and height, yaw about the root, tilt about the root by the lean, then move to the root
        public static Vector3 Place(Vector3 positionOS, Vector3 root, float yaw, float width, float height, Vector2 lean)
        {
            Vector3 scaled = new Vector3(positionOS.x * width, positionOS.y * height, positionOS.z * width);
            return root + Tilt(Yaw(scaled, yaw), lean);
        }

        // The normal takes the inverse of the scale, then the same yaw and tilt as the position
        public static Vector3 PlaceNormal(Vector3 normalOS, float yaw, float width, float height, Vector2 lean)
        {
            Vector3 scaled = new Vector3(normalOS.x / width, normalOS.y / height, normalOS.z / width);
            return Tilt(Yaw(scaled, yaw), lean).normalized;
        }

        // Yaw in radians about +Y, the turn Quaternion.Euler(0, yaw, 0) makes
        static Vector3 Yaw(Vector3 v, float yaw)
        {
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            return new Vector3(v.x * cos + v.z * sin, v.y, v.z * cos - v.x * sin);
        }
    }
}
