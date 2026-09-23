using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Shared meshes without textures, baked by PrimitiveMeshBaker. Cylinder height and sphere diameter are one.
    public class HLPrimitiveMeshes : ScriptableObject
    {
        public Mesh sphere;
        public Mesh capsule;
        public Mesh cone;
        public Mesh cylinder;
        public Mesh torus;
        public Mesh thinTorus;
        public Mesh bladeCone;
        public Mesh pyramid;
        public Mesh star;
        public Mesh boulder;
        public Mesh disc;
        public Mesh annulus;

        // Recipes only author torus parts at the baked 0.2 tube ratio
        public Mesh GetMesh(HLPrimitive primitive)
        {
            switch (primitive)
            {
                case HLPrimitive.Capsule:
                    return capsule;
                case HLPrimitive.Cone:
                    return cone;
                case HLPrimitive.Torus:
                    return torus;
                case HLPrimitive.CylinderSegment:
                    return cylinder;
                default:
                    return sphere;
            }
        }

        // removed in D2: the runtime cache below, once Grass and Environment read the baked fields
        static readonly Dictionary<(HLPrimitive, int, int, float), Mesh> _cache =
            new Dictionary<(HLPrimitive, int, int, float), Mesh>();
        static int _owners;

        // removed in D2
        public static void Retain()
        {
            _owners++;
        }

        // removed in D2
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

        // removed in D2
        public static Mesh Get(HLPrimitive primitive, int radialSegments = 10, int axialSegments = 6,
            float torusTubeRatio = 0.25f)
        {
            if ((int)primitive < 0 || (int)primitive > 4 || radialSegments < 3 || radialSegments > 128
                || axialSegments < 2 || axialSegments > 128
                || !float.IsFinite(torusTubeRatio) || torusTubeRatio <= 0f || torusTubeRatio >= 1f)
            {
                Debug.LogError("[HLPrimitiveMeshes] Invalid mesh settings.");
                return null;
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

        // removed in D2
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

        // removed in D2
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

        public static Transform Geometry(string name, Transform parent, Mesh mesh, Material material, Color colour,
            float glow = 0f)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
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
