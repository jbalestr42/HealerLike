using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    /// <summary>Shared, texture-free meshes. Cylinder height and sphere diameter are one.</summary>
    public static class HLPrimitiveMeshes
    {
        static readonly Dictionary<(HLPrimitive, int, int, float), Mesh> Cache = new Dictionary<(HLPrimitive, int, int, float), Mesh>();
        static int owners;
        internal static void Retain() => owners++;
        internal static void Release()
        {
            if (owners == 0) return;
            if (--owners == 0) ReleaseAll();
        }
        public static Mesh Get(HLPrimitive primitive, int radialSegments = 10, int axialSegments = 6, float torusTubeRatio = .25f)
        {
            if ((int)primitive < 0 || (int)primitive > 4 || radialSegments < 3 || radialSegments > 128 || axialSegments < 2 || axialSegments > 128
                || !HLChainSolver.Finite(torusTubeRatio) || torusTubeRatio <= 0 || torusTubeRatio >= 1) throw new ArgumentException("Invalid mesh settings.");
            var key = (primitive, radialSegments, axialSegments, torusTubeRatio);
            if (Cache.TryGetValue(key, out var cached) && cached) return cached;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            int rows = primitive == HLPrimitive.Cone || primitive == HLPrimitive.CylinderSegment ? 1 : axialSegments;
            if (primitive == HLPrimitive.Capsule) rows = axialSegments * 2 + 1;
            for (int j = 0; j <= rows; j++)
            {
                float v = (float)j / rows;
                for (int i = 0; i <= radialSegments; i++)
                {
                    float angle = 2 * Mathf.PI * i / radialSegments;
                    Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 point, normal;
                    if (primitive == HLPrimitive.Torus)
                    {
                        float minor = .5f * torusTubeRatio / (1 + torusTubeRatio), major = .5f - minor;
                        float phi = v * Mathf.PI * 2;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        point = radial * major + normal * minor;
                    }
                    else if (primitive == HLPrimitive.Cone)
                    { point = radial * (.5f * (1 - v)) + Vector3.up * (v - .5f); normal = (radial + Vector3.up * .5f).normalized; }
                    else if (primitive == HLPrimitive.CylinderSegment)
                    { point = radial * .5f + Vector3.up * (v - .5f); normal = radial; }
                    else if (primitive == HLPrimitive.Capsule)
                    {
                        bool bottom = j <= axialSegments;
                        float phi = bottom ? -Mathf.PI / 2 + (float)j / axialSegments * Mathf.PI / 2
                            : (float)(j - axialSegments - 1) / axialSegments * Mathf.PI / 2;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        // Radius .25, cylindrical middle .5. Normalize XZ diameter to one.
                        point = normal * .25f + Vector3.up * (bottom ? -.25f : .25f);
                        point.x *= 2; point.z *= 2;
                        normal = new Vector3(normal.x * .5f, normal.y, normal.z * .5f).normalized;
                    }
                    else
                    {
                        float phi = -Mathf.PI / 2 + v * Mathf.PI;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        point = normal * .5f;
                    }
                    vertices.Add(point); normals.Add(normal.normalized);
                }
            }
            for (int j = 0; j < rows; j++) for (int i = 0; i < radialSegments; i++)
            {
                int a = j * (radialSegments + 1) + i, b = a + radialSegments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            if (primitive == HLPrimitive.Cone || primitive == HLPrimitive.CylinderSegment)
            {
                AddCap(-.5f, .5f, false, radialSegments, vertices, normals, triangles);
                if (primitive == HLPrimitive.CylinderSegment) AddCap(.5f, .5f, true, radialSegments, vertices, normals, triangles);
            }
            var mesh = new Mesh { name = "HL" + primitive, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }
        static void AddCap(float y, float radius, bool top, int n, List<Vector3> v, List<Vector3> normals, List<int> t)
        {
            int center = v.Count;
            Vector3 normal = top ? Vector3.up : Vector3.down;
            v.Add(new Vector3(0, y, 0)); normals.Add(normal);
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius)); normals.Add(normal);
            }
            for (int i = 0; i < n; i++)
            { t.Add(center); t.Add(center + 1 + (top ? i + 1 : i)); t.Add(center + 1 + (top ? i : i + 1)); }
        }
        public static void ReleaseAll()
        {
            if (owners != 0) return;
            foreach (var mesh in Cache.Values) DestroyOwned(mesh);
            Cache.Clear();
        }
        internal static void DestroyOwned(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
        internal static Transform Geometry(string name, Transform parent, HLPrimitive kind, Material material, Color colour, float ratio = .25f, float glow = 0)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = Get(kind, kind == HLPrimitive.CylinderSegment ? 6 : 12, 6, ratio);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Brighten(colour, glow));
            renderer.SetPropertyBlock(block);
            return go.transform;
        }
        internal static Color Brighten(Color colour, float glow)
        {
            float brightness = 1 + Mathf.Max(0, glow);
            return new Color(colour.r * brightness, colour.g * brightness, colour.b * brightness, colour.a);
        }
        internal static void Segment(Transform segment, Vector3 a, Vector3 b, float radius)
        {
            Vector3 delta = b - a;
            segment.SetPositionAndRotation((a + b) * .5f, delta.sqrMagnitude > 1e-12f ? Quaternion.FromToRotation(Vector3.up, delta) : Quaternion.identity);
            float parentScale = segment.parent ? segment.parent.lossyScale.x : 1;
            segment.localScale = new Vector3(radius * 2, delta.magnitude, radius * 2) / parentScale;
        }
    }
}
