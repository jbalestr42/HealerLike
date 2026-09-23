using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // One grass tuft: a slender four-sided spike, widest a little above its base, closed by a base cap.
    // x and z are in tuft widths and y in tuft heights: the base spans -0.5..0.5 and the tip is at y 1.
    // Vertex colour red marks the tip band, the top fifth, which takes the palette tip green.
    // Place mirrors HLPlaceGrassBlade in GrassInstancing.hlsl, a rigid transform with no bending.
    public static class GrassTuft
    {
        public static readonly int IndexCount = 66;
        public static readonly float ShoulderHeight = 0.3f;
        public static readonly float ShoulderWidth = 1.12f;
        public static readonly float TipBand = 0.8f;

        public static Mesh CreateMesh()
        {
            Vector3 apex = Vector3.up;
            Vector3[] bases = new Vector3[4];
            Vector3[] shoulders = new Vector3[4];
            Vector3[] bands = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                // Corners in the order of FacetedMeshes.CreatePyramid, so the same winding faces out
                float x = i == 1 || i == 2 ? 0.5f : -0.5f;
                float z = i >= 2 ? 0.5f : -0.5f;
                bases[i] = new Vector3(x, 0f, z);
                shoulders[i] = new Vector3(x * ShoulderWidth, ShoulderHeight, z * ShoulderWidth);
                bands[i] = Vector3.Lerp(shoulders[i], apex, (TipBand - ShoulderHeight) / (1f - ShoulderHeight));
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                AddTriangle(vertices, normals, colors, bases[i], shoulders[i], shoulders[j], 0f);
                AddTriangle(vertices, normals, colors, bases[i], shoulders[j], bases[j], 0f);
                AddTriangle(vertices, normals, colors, shoulders[i], bands[i], bands[j], 0f);
                AddTriangle(vertices, normals, colors, shoulders[i], bands[j], shoulders[j], 0f);
                AddTriangle(vertices, normals, colors, bands[i], apex, bands[j], 1f);
            }

            AddTriangle(vertices, normals, colors, bases[0], bases[1], bases[2], 0f);
            AddTriangle(vertices, normals, colors, bases[0], bases[2], bases[3], 0f);

            int[] triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            Mesh mesh = new Mesh { name = "Tuft" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // One flat-shaded facet; tip is 1 on the tip band and 0 elsewhere
        static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<Color> colors, Vector3 a, Vector3 b, Vector3 c, float tip)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Color color = new Color(tip, 0f, 0f, 1f);
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            for (int i = 0; i < 3; i++)
            {
                normals.Add(normal);
                colors.Add(color);
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
