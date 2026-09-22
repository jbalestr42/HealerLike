using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    [Serializable]
    public struct HLStoneSettings
    {
        public float Size, Elongation, DepthRatio, Roughness;
        public int Subdivisions;
    }

    // Arrays belong to this result; consumers must treat them as immutable.
    public readonly struct HLStoneMeshData
    {
        public readonly Vector3[] Vertices, Normals, WeldedVertices;
        public readonly int[] Indices, WeldedIndices;
        public readonly Bounds Bounds;
        public HLStoneMeshData(Vector3[] vertices, Vector3[] normals, int[] indices, Bounds bounds,
            Vector3[] weldedVertices = null, int[] weldedIndices = null)
        {
            Vertices = vertices; Normals = normals; Indices = indices; Bounds = bounds;
            WeldedVertices = weldedVertices; WeldedIndices = weldedIndices;
        }
    }

    public static class HLStoneMesh
    {
        public const int GeneratorVersion = 1;
        public static void Validate(in HLStoneSettings s)
        {
            Range(s.Size, .02f, 8, nameof(s.Size));
            Range(s.Elongation, .25f, 5, nameof(s.Elongation));
            Range(s.DepthRatio, .25f, 2, nameof(s.DepthRatio));
            Range(s.Roughness, 0, .18f, nameof(s.Roughness));
            TriangleCount(s.Subdivisions);
        }
        static void Range(float v, float min, float max, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < min || v > max)
                throw new ArgumentOutOfRangeException(name);
        }
        public static int TriangleCount(int subdivisions)
        {
            if (subdivisions < 0 || subdivisions > 2) throw new ArgumentOutOfRangeException(nameof(subdivisions));
            return 20 << (subdivisions * 2);
        }
        public static int VertexCount(int subdivisions) => TriangleCount(subdivisions) * 3;

        public static HLStoneMeshData Generate(uint seed, in HLStoneSettings s)
        {
            Validate(s);
            float t = (1 + Mathf.Sqrt(5)) * .5f;
            var points = new List<Vector3> {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1) };
            for (int i = 0; i < points.Count; i++) points[i] = points[i].normalized;
            var faces = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int n = 0; n < s.Subdivisions; n++)
            {
                var edges = new Dictionary<ulong, int>();
                var next = new List<int>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i], b = faces[i+1], c = faces[i+2];
                    int ab = Midpoint(a,b,points,edges), bc = Midpoint(b,c,points,edges), ca = Midpoint(c,a,points,edges);
                    next.AddRange(new[] { a,ab,ca, b,bc,ab, c,ca,bc, ab,bc,ca });
                }
                faces = next;
            }
            var displaced = new Vector3[points.Count];
            var random = new HLStoneRandom(seed);
            var noise = new float[points.Count];
            for (int i = 0; i < noise.Length; i++) noise[i] = random.Next01() * 2 - 1;
            float roughness = s.Roughness;
            for (int attempt = 0; ; attempt++)
            {
                for (int i = 0; i < points.Count; i++) displaced[i] = points[i] * (1 + noise[i] * roughness);
                if (ValidFaces(displaced, faces)) break;
                if (attempt >= 4) roughness = 0; else roughness *= .5f;
                if (attempt > 5) throw new InvalidOperationException("Invalid stone template");
            }
            Vector3 scale = new Vector3(s.Size, s.Size * s.Elongation, s.Size * s.DepthRatio) * .5f;
            for (int i = 0; i < displaced.Length; i++) displaced[i] = Vector3.Scale(displaced[i], scale);
            var vertices = new Vector3[faces.Count];
            var normals = new Vector3[faces.Count];
            var indices = new int[faces.Count];
            var bounds = new Bounds(displaced[0], Vector3.zero);
            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a = displaced[faces[i]], b = displaced[faces[i+1]], c = displaced[faces[i+2]];
                Vector3 cross = Vector3.Cross(b-a,c-a);
                if (!(cross.sqrMagnitude > 0) || !float.IsFinite(cross.sqrMagnitude) || Vector3.Dot(cross,a+b+c) <= 0)
                    throw new InvalidOperationException("Degenerate scaled stone face");
                Vector3 normal = cross / Mathf.Sqrt(cross.sqrMagnitude);
                for (int j = 0; j < 3; j++)
                {
                    vertices[i+j] = displaced[faces[i+j]]; normals[i+j] = normal; indices[i+j] = i+j;
                    bounds.Encapsulate(vertices[i+j]);
                }
            }
            return new HLStoneMeshData(vertices, normals, indices, bounds, displaced, faces.ToArray());
        }
        static int Midpoint(int a, int b, List<Vector3> p, Dictionary<ulong,int> cache)
        {
            ulong key = ((ulong)(uint)System.Math.Min(a,b) << 32) | (uint)System.Math.Max(a,b);
            if (cache.TryGetValue(key, out int index)) return index;
            index = p.Count; p.Add((p[a]+p[b]).normalized); cache.Add(key,index); return index;
        }
        static bool ValidFaces(Vector3[] p, List<int> faces)
        {
            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a=p[faces[i]], b=p[faces[i+1]], c=p[faces[i+2]], cross=Vector3.Cross(b-a,c-a);
                if (cross.magnitude <= 1e-10f || Vector3.Dot(cross,a+b+c) <= 0) return false;
            }
            return true;
        }
        public static Mesh CreateMesh(uint seed, in HLStoneSettings settings) => CreateMesh(Generate(seed, settings));
        public static Mesh CreateMesh(in HLStoneMeshData data)
        {
            var mesh = new Mesh { name = "HLStone" };
            mesh.vertices = data.Vertices; mesh.normals = data.Normals; mesh.triangles = data.Indices; mesh.bounds = data.Bounds;
            return mesh;
        }
    }
}
