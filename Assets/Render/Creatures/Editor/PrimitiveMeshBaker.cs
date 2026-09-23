using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Creatures
{
    // Writes the shared primitive meshes as assets, and the PrimitiveMeshes asset that references them
    public static class PrimitiveMeshBaker
    {
        static readonly string creaturesFolder = "Assets/Render/Creatures";
        static readonly string meshesFolder = "Assets/Render/Creatures/Meshes";
        static readonly string meshesAssetPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

        [MenuItem("Tools/Render/Bake Primitive Meshes")]
        public static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(meshesFolder))
            {
                AssetDatabase.CreateFolder(creaturesFolder, "Meshes");
            }

            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);
            if (meshes == null)
            {
                meshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
                AssetDatabase.CreateAsset(meshes, meshesAssetPath);
            }

            meshes.sphere = Save(CreateRevolved("Sphere", Primitive.Sphere, 12, 6, 0.2f));
            meshes.capsule = Save(CreateRevolved("Capsule", Primitive.Capsule, 12, 6, 0.2f));
            meshes.cone = Save(CreateRevolved("Cone", Primitive.Cone, 12, 6, 0.2f));
            meshes.cylinder = Save(CreateRevolved("Cylinder", Primitive.CylinderSegment, 6, 6, 0.2f));
            meshes.torus = Save(CreateRevolved("Torus", Primitive.Torus, 12, 6, 0.2f));
            meshes.thinTorus = Save(CreateThinTorus());
            meshes.bladeCone = Save(CreateRevolved("BladeCone", Primitive.Cone, GrassField.BladeSides, 2, 0.2f));
            meshes.pyramid = Save(CreatePyramid());
            meshes.star = Save(CreateStar());
            meshes.boulder = Save(CreateBoulder());
            meshes.disc = Save(CreateDisc(32));
            meshes.annulus = Save(CreateAnnulus(128));

            EditorUtility.SetDirty(meshes);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PrimitiveMeshBaker] Baked 12 meshes into {meshesFolder}");
        }

        static Mesh Save(Mesh mesh)
        {
            string path = meshesFolder + "/" + mesh.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // Sphere, capsule, cone, cylinder and torus around the Y axis, one unit high and wide
        static Mesh CreateRevolved(string name, Primitive primitive, int radialSegments, int axialSegments,
            float torusTubeRatio)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            int rows = axialSegments;
            if (primitive == Primitive.Cone || primitive == Primitive.CylinderSegment)
            {
                rows = 1;
            }
            else if (primitive == Primitive.Capsule)
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
                    if (primitive == Primitive.Torus)
                    {
                        float minor = 0.5f * torusTubeRatio / (1f + torusTubeRatio);
                        float major = 0.5f - minor;
                        float phi = v * Mathf.PI * 2f;
                        normal = radial * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);
                        point = radial * major + normal * minor;
                    }
                    else if (primitive == Primitive.Cone)
                    {
                        point = radial * (0.5f * (1f - v)) + Vector3.up * (v - 0.5f);
                        normal = (radial + Vector3.up * 0.5f).normalized;
                    }
                    else if (primitive == Primitive.CylinderSegment)
                    {
                        point = radial * 0.5f + Vector3.up * (v - 0.5f);
                        normal = radial;
                    }
                    else if (primitive == Primitive.Capsule)
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

            if (primitive == Primitive.Cone || primitive == Primitive.CylinderSegment)
            {
                AddCap(-0.5f, 0.5f, false, radialSegments, vertices, normals, triangles);
                if (primitive == Primitive.CylinderSegment)
                {
                    AddCap(0.5f, 0.5f, true, radialSegments, vertices, normals, triangles);
                }
            }

            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
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

        // The spell ring: one unit wide, a tube of 0.05 of that width
        static Mesh CreateThinTorus()
        {
            int rings = 32;
            int sides = 6;
            Vector3[] vertices = new Vector3[rings * sides];
            int[] triangles = new int[rings * sides * 6];
            for (int i = 0; i < rings; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    float a = i * Mathf.PI * 2f / rings;
                    float b = j * Mathf.PI * 2f / sides;
                    int k = i * sides + j;
                    float distance = 0.5f + 0.025f * Mathf.Cos(b);
                    vertices[k] = new Vector3(distance * Mathf.Cos(a), 0.025f * Mathf.Sin(b), distance * Mathf.Sin(a));

                    int next = ((i + 1) % rings) * sides + j;
                    int side = i * sides + (j + 1) % sides;
                    int diagonal = ((i + 1) % rings) * sides + (j + 1) % sides;
                    int offset = k * 6;
                    triangles[offset] = k;
                    triangles[offset + 1] = side;
                    triangles[offset + 2] = next;
                    triangles[offset + 3] = side;
                    triangles[offset + 4] = diagonal;
                    triangles[offset + 5] = next;
                }
            }

            return CreateMesh("ThinTorus", vertices, triangles);
        }

        // The stones' flat-shaded four-sided cone, base on the ground
        static Mesh CreatePyramid()
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

        // Two-sided planar fan: eight long rays alternating with short notches
        static Mesh CreateStar()
        {
            Vector3[] vertices = new Vector3[34];
            int[] triangles = new int[96];
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                float radius = i % 2 == 0 ? 1f : 0.32f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                vertices[i + 18] = vertices[i + 1];

                int next = (i + 1) % 16 + 1;
                int offset = i * 6;
                triangles[offset] = 0;
                triangles[offset + 1] = i + 1;
                triangles[offset + 2] = next;
                triangles[offset + 3] = 17;
                triangles[offset + 4] = next + 17;
                triangles[offset + 5] = i + 18;
            }

            return CreateMesh("Star", vertices, triangles);
        }

        // Flat-shaded octahedron, kept asymmetric on purpose
        static Mesh CreateBoulder()
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

            return CreateMesh("Boulder", vertices, triangles);
        }

        // Flat disc of radius one on the ground plane
        static Mesh CreateDisc(int segments)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            return CreateMesh("Disc", vertices, triangles);
        }

        // Thin ring of radius one, the uv carries the angle and the side across the band
        static Mesh CreateAnnulus(int segments)
        {
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i % segments) * Mathf.PI * 2f / segments;
                for (int side = 0; side < 2; side++)
                {
                    float radius = 1f + (side - 0.5f) * 0.025f;
                    vertices[i * 2 + side] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    uv[i * 2 + side] = new Vector2(angle, side - 0.5f);
                }

                if (i == segments)
                {
                    continue;
                }

                int vertex = i * 2;
                int offset = i * 6;
                triangles[offset] = vertex;
                triangles[offset + 1] = vertex + 2;
                triangles[offset + 2] = vertex + 1;
                triangles[offset + 3] = vertex + 1;
                triangles[offset + 4] = vertex + 2;
                triangles[offset + 5] = vertex + 3;
            }

            Mesh mesh = new Mesh { name = "Annulus" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
