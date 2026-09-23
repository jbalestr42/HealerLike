using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Shared meshes without textures. Cylinder height and sphere diameter are one.
    public static class HLPrimitiveMeshes
    {
        static readonly Dictionary<(HLPrimitive, int, int, float), Mesh> _cache =
            new Dictionary<(HLPrimitive, int, int, float), Mesh>();
        static int _owners;

        public static void Retain()
        {
            _owners++;
        }

        public static void Release()
        {
            if (_owners == 0)
            {
                return;
            }

            if (--_owners == 0)
            {
                ReleaseAll();
            }
        }

        public static Mesh Get(HLPrimitive primitive, int radialSegments = 10, int axialSegments = 6,
            float torusTubeRatio = 0.25f)
        {
            if ((int)primitive < 0 || (int)primitive > 4 || radialSegments < 3 || radialSegments > 128
                || axialSegments < 2 || axialSegments > 128
                || !HLChainSolver.Finite(torusTubeRatio) || torusTubeRatio <= 0f || torusTubeRatio >= 1f)
            {
                throw new ArgumentException("Invalid mesh settings.");
            }

            (HLPrimitive, int, int, float) key = (primitive, radialSegments, axialSegments, torusTubeRatio);
            if (_cache.TryGetValue(key, out Mesh cached) && cached)
            {
                return cached;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            int rows = primitive == HLPrimitive.Cone || primitive == HLPrimitive.CylinderSegment ? 1 : axialSegments;
            if (primitive == HLPrimitive.Capsule)
            {
                rows = axialSegments * 2 + 1;
            }

            for (int j = 0; j <= rows; j++)
            {
                float v = (float)j / rows;
                for (int i = 0; i <= radialSegments; i++)
                {
                    float angle = 2f * Mathf.PI * i / radialSegments;
                    Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 point;
                    Vector3 normal;
                    if (primitive == HLPrimitive.Torus)
                    {
                        float minor = 0.5f * torusTubeRatio / (1f + torusTubeRatio);
                        float major = 0.5f - minor;
                        float phi = v * Mathf.PI * 2f;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        point = radial * major + normal * minor;
                    }
                    else if (primitive == HLPrimitive.Cone)
                    {
                        point = radial * (0.5f * (1f - v)) + Vector3.up * (v - 0.5f);
                        normal = (radial + Vector3.up * 0.5f).normalized;
                    }
                    else if (primitive == HLPrimitive.CylinderSegment)
                    {
                        point = radial * 0.5f + Vector3.up * (v - 0.5f);
                        normal = radial;
                    }
                    else if (primitive == HLPrimitive.Capsule)
                    {
                        bool isBottom = j <= axialSegments;
                        float phi;
                        if (isBottom)
                        {
                            phi = -Mathf.PI / 2f + (float)j / axialSegments * Mathf.PI / 2f;
                        }
                        else
                        {
                            phi = (float)(j - axialSegments - 1) / axialSegments * Mathf.PI / 2f;
                        }

                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        // Radius 0.25 and a 0.5 cylindrical middle, then the XZ diameter is scaled back to one
                        point = normal * 0.25f + Vector3.up * (isBottom ? -0.25f : 0.25f);
                        point.x *= 2f;
                        point.z *= 2f;
                        normal = new Vector3(normal.x * 0.5f, normal.y, normal.z * 0.5f).normalized;
                    }
                    else
                    {
                        float phi = -Mathf.PI / 2f + v * Mathf.PI;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        point = normal * 0.5f;
                    }

                    vertices.Add(point);
                    normals.Add(normal.normalized);
                }
            }

            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < radialSegments; i++)
                {
                    int a = j * (radialSegments + 1) + i;
                    int b = a + radialSegments + 1;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }

            if (primitive == HLPrimitive.Cone || primitive == HLPrimitive.CylinderSegment)
            {
                AddCap(-0.5f, 0.5f, false, radialSegments, vertices, normals, triangles);
                if (primitive == HLPrimitive.CylinderSegment)
                {
                    AddCap(0.5f, 0.5f, true, radialSegments, vertices, normals, triangles);
                }
            }

            Mesh mesh = new Mesh { name = "HL" + primitive, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            _cache[key] = mesh;
            return mesh;
        }

        public static void ReleaseAll()
        {
            if (_owners != 0)
            {
                return;
            }

            foreach (Mesh mesh in _cache.Values)
            {
                DestroyOwned(mesh);
            }

            _cache.Clear();
        }

        public static void DestroyOwned(UnityEngine.Object value)
        {
            if (!value)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        public static Transform Geometry(string name, Transform parent, HLPrimitive kind, Material material,
            Color colour, float ratio = 0.25f, float glow = 0f)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            int radialSegments = kind == HLPrimitive.CylinderSegment ? 6 : 12;
            go.GetComponent<MeshFilter>().sharedMesh = Get(kind, radialSegments, 6, ratio);
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Brighten(colour, glow));
            renderer.SetPropertyBlock(block);
            return go.transform;
        }

        public static Color Brighten(Color colour, float glow)
        {
            float brightness = 1f + Mathf.Max(0f, glow);
            return new Color(colour.r * brightness, colour.g * brightness, colour.b * brightness, colour.a);
        }

        public static void Segment(Transform segment, Vector3 a, Vector3 b, float radius)
        {
            Vector3 delta = b - a;
            Quaternion rotation = Quaternion.identity;
            if (delta.sqrMagnitude > 0.000000000001f)
            {
                rotation = Quaternion.FromToRotation(Vector3.up, delta);
            }

            segment.SetPositionAndRotation((a + b) * 0.5f, rotation);
            float parentScale = segment.parent ? segment.parent.lossyScale.x : 1f;
            segment.localScale = new Vector3(radius * 2f, delta.magnitude, radius * 2f) / parentScale;
        }

        static void AddCap(float y, float radius, bool isTop, int count, List<Vector3> vertices, List<Vector3> normals,
            List<int> triangles)
        {
            int center = vertices.Count;
            Vector3 normal = isTop ? Vector3.up : Vector3.down;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(normal);
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                normals.Add(normal);
            }

            for (int i = 0; i < count; i++)
            {
                triangles.Add(center);
                triangles.Add(center + 1 + (isTop ? i + 1 : i));
                triangles.Add(center + 1 + (isTop ? i : i + 1));
            }
        }
    }
}
