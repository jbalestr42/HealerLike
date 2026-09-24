using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // One grass tuft: a four-sided pyramid, flat shaded, open at its base, which sits in the ground on the socle.
    // x and z are in tuft widths and y in tuft heights: the base spans -0.5..0.5 and the apex is at y 1.
    // The socle is a flat octagon on the ground under the tuft, in the same widths.
    // Place mirrors HLPlaceGrassBlade in GrassInstancing.hlsl, a rigid transform with no bending.
    public static class GrassTuft
    {
        public static readonly int IndexCount = 12;
        public static readonly int SocleIndexCount = 24;
        public static readonly int SocleSides = 8;
        // 0.36 of socle radius for a 0.29 wide pyramid
        public static readonly float SocleRadius = 0.36f / 0.29f;

        public static Mesh CreateMesh()
        {
            Vector3 apex = Vector3.up;
            Vector3[] bases = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                // Corners in the order of FacetedMeshes.CreatePyramid, so the same winding faces out
                float x = i == 1 || i == 2 ? 0.5f : -0.5f;
                float z = i >= 2 ? 0.5f : -0.5f;
                bases[i] = new Vector3(x, 0f, z);
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < 4; i++)
            {
                AddTriangle(vertices, normals, bases[i], apex, bases[(i + 1) % 4]);
            }

            return CreateMesh("Tuft", vertices, normals);
        }

        // A fan around the root, facing up
        public static Mesh CreateSocle()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < SocleSides; i++)
            {
                AddTriangle(vertices, normals, Vector3.zero, SocleCorner(i + 1), SocleCorner(i));
            }

            return CreateMesh("Socle", vertices, normals);
        }

        static Vector3 SocleCorner(int i)
        {
            float angle = i * Mathf.PI * 2f / SocleSides;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SocleRadius;
        }

        static Mesh CreateMesh(string name, List<Vector3> vertices, List<Vector3> normals)
        {
            int[] triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // One flat-shaded facet
        static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            for (int i = 0; i < 3; i++)
            {
                normals.Add(normal);
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
        public static Vector3 Place(Vector3 positionOS, Vector3 root, float yaw, float width, float height,
                                    Vector2 lean)
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
