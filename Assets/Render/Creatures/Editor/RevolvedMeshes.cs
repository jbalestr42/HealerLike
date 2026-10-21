using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Meshes of revolution around the Y axis for the primitive baker
    public static class RevolvedMeshes
    {
        // Sphere, capsule, cone, cylinder and torus around the Y axis, one unit high and wide
        public static Mesh Create(
            string name,
            Primitive primitive,
            int radialSegments,
            int axialSegments,
            float torusTubeRatio
        )
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
                        float capCentre = 0.25f;
                        if (isBottom)
                        {
                            capCentre = -0.25f;
                        }

                        point = normal * 0.25f + Vector3.up * capCentre;
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

            return PrimitiveMeshBaker.CreateMesh(
                name,
                vertices.ToArray(),
                triangles.ToArray(),
                normals.ToArray(),
                null
            );
        }

        static void AddCap(
            float y,
            float radius,
            bool isTop,
            int count,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles
        )
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
                // A top cap winds the other way round so it faces up
                int first = center + 1 + i;
                int second = center + 2 + i;
                if (isTop)
                {
                    first = center + 2 + i;
                    second = center + 1 + i;
                }

                triangles.Add(center);
                triangles.Add(first);
                triangles.Add(second);
            }
        }
    }
}
