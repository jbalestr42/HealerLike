using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class StoneMesh
    {
        public static readonly int GeneratorVersion = 1;

        // A face whose edge cross product is shorter than this has no area
        static readonly float degenerateCross = 0.0000000001f;

        public static bool IsValid(StoneSettings settings)
        {
            return InRange(settings.size, 0.02f, 8f) && InRange(settings.elongation, 0.25f, 5f)
                && InRange(settings.depthRatio, 0.25f, 2f) && InRange(settings.roughness, 0f, 0.18f)
                && settings.subdivisions >= 0 && settings.subdivisions <= 2;
        }

        static bool InRange(float value, float min, float max)
        {
            return float.IsFinite(value) && value >= min && value <= max;
        }

        // Logs and returns empty data when the settings are out of range
        public static StoneMeshData Generate(uint seed, StoneSettings settings)
        {
            StoneMeshData data;
            if (!TryGenerate(seed, settings, out data))
            {
                Debug.LogError($"[StoneMesh] No stone for seed {seed}, the settings are out of range.");
            }
            return data;
        }

        public static bool TryGenerate(uint seed, StoneSettings settings, out StoneMeshData data)
        {
            data = default;
            if (!IsValid(settings))
            {
                return false;
            }

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> points = new List<Vector3>
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].normalized;
            }

            List<int> faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            for (int n = 0; n < settings.subdivisions; n++)
            {
                Dictionary<ulong, int> edges = new Dictionary<ulong, int>();
                List<int> next = new List<int>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i];
                    int b = faces[i + 1];
                    int c = faces[i + 2];
                    int ab = Midpoint(a, b, points, edges);
                    int bc = Midpoint(b, c, points, edges);
                    int ca = Midpoint(c, a, points, edges);
                    next.AddRange(new int[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }

            Vector3[] displaced = new Vector3[points.Count];
            SeededRandom random = new SeededRandom(seed);
            float[] noise = new float[points.Count];
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = random.Next01() * 2f - 1f;
            }

            float roughness = settings.roughness;
            for (int attempt = 0; ; attempt++)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    displaced[i] = points[i] * (1f + noise[i] * roughness);
                }
                if (ValidFaces(displaced, faces))
                {
                    break;
                }

                if (attempt >= 4)
                {
                    roughness = 0f;
                }
                else
                {
                    roughness *= 0.5f;
                }
                if (attempt > 5)
                {
                    return false;
                }
            }

            float height = settings.size * settings.elongation;
            float depth = settings.size * settings.depthRatio;
            Vector3 size = new Vector3(settings.size, height, depth);
            Vector3 scale = size * 0.5f;
            for (int i = 0; i < displaced.Length; i++)
            {
                displaced[i] = Vector3.Scale(displaced[i], scale);
            }

            Vector3[] vertices = new Vector3[faces.Count];
            Vector3[] normals = new Vector3[faces.Count];
            int[] indices = new int[faces.Count];
            Bounds bounds = new Bounds(displaced[0], Vector3.zero);
            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a = displaced[faces[i]];
                Vector3 b = displaced[faces[i + 1]];
                Vector3 c = displaced[faces[i + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (!(cross.sqrMagnitude > 0f) || !float.IsFinite(cross.sqrMagnitude)
                    || Vector3.Dot(cross, a + b + c) <= 0f)
                {
                    return false;
                }

                Vector3 normal = cross / Mathf.Sqrt(cross.sqrMagnitude);
                for (int j = 0; j < 3; j++)
                {
                    vertices[i + j] = displaced[faces[i + j]];
                    normals[i + j] = normal;
                    indices[i + j] = i + j;
                    bounds.Encapsulate(vertices[i + j]);
                }
            }
            data = new StoneMeshData(vertices, normals, indices, bounds, displaced, faces.ToArray());
            return true;
        }

        static int Midpoint(int a, int b, List<Vector3> points, Dictionary<ulong, int> cache)
        {
            ulong key = ((ulong)(uint)System.Math.Min(a, b) << 32) | (uint)System.Math.Max(a, b);
            if (cache.TryGetValue(key, out int index))
            {
                return index;
            }

            index = points.Count;
            points.Add((points[a] + points[b]).normalized);
            cache.Add(key, index);
            return index;
        }

        static bool ValidFaces(Vector3[] points, List<int> faces)
        {
            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a = points[faces[i]];
                Vector3 b = points[faces[i + 1]];
                Vector3 c = points[faces[i + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.magnitude <= degenerateCross || Vector3.Dot(cross, a + b + c) <= 0f)
                {
                    return false;
                }
            }
            return true;
        }

        public static Mesh CreateMesh(uint seed, StoneSettings settings)
        {
            StoneMeshData data;
            if (!TryGenerate(seed, settings, out data))
            {
                Debug.LogError($"[StoneMesh] No stone mesh for seed {seed}, the settings are out of range.");
                return null;
            }
            return CreateMesh(data);
        }

        public static Mesh CreateMesh(StoneMeshData data)
        {
            Mesh mesh = new Mesh { name = "Stone" };
            mesh.vertices = data.vertices;
            mesh.normals = data.normals;
            mesh.triangles = data.indices;
            mesh.bounds = data.bounds;
            return mesh;
        }
    }
}
