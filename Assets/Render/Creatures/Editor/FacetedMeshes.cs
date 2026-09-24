using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Flat-shaded shapes for the primitive baker
    public static class FacetedMeshes
    {
        // The open pyramid is the grass tuft, four sides; the socle under it is a flat fan
        public static readonly int TuftIndexCount = 12;
        public static readonly int SocleIndexCount = 24;
        public static readonly int SocleSides = 8;
        // 0.36 of socle radius for a 0.29 wide pyramid
        public static readonly float SocleRadius = 0.36f / 0.29f;

        // A flat-shaded four-sided cone, base on the ground spanning -0.5..0.5 and apex at y 1. The stones'
        // pyramid has its base; the grass tuft is open there, since it sits in the ground on the socle.
        public static Mesh CreatePyramid(string name, bool hasBase)
        {
            Vector3[] points =
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                Vector3.up
            };
            int[] sides = { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 };
            int[] bottom = { 0, 1, 2, 0, 2, 3 };
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < sides.Length; i += 3)
            {
                AddFacet(vertices, normals, points[sides[i]], points[sides[i + 1]], points[sides[i + 2]]);
            }

            if (hasBase)
            {
                for (int i = 0; i < bottom.Length; i += 3)
                {
                    AddFacet(vertices, normals, points[bottom[i]], points[bottom[i + 1]], points[bottom[i + 2]]);
                }
            }

            return CreateFacets(name, vertices, normals);
        }

        // A flat octagon fan around the root, facing up, in tuft widths
        public static Mesh CreateSocle()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < SocleSides; i++)
            {
                AddFacet(vertices, normals, Vector3.zero, SocleCorner(i + 1), SocleCorner(i));
            }

            return CreateFacets("Socle", vertices, normals);
        }

        static Vector3 SocleCorner(int i)
        {
            float angle = i * Mathf.PI * 2f / SocleSides;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SocleRadius;
        }

        // One flat-shaded facet, its normal from its winding
        static void AddFacet(List<Vector3> vertices, List<Vector3> normals, Vector3 a, Vector3 b, Vector3 c)
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

        static Mesh CreateFacets(string name, List<Vector3> vertices, List<Vector3> normals)
        {
            int[] triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            return PrimitiveMeshBaker.CreateMesh(name, vertices.ToArray(), triangles, normals.ToArray(), null);
        }

        // Tall leaf with a diamond section: widest at the base cap, tapering to a sharp tip. One unit on each axis.
        public static Mesh CreateLeaf()
        {
            float[] heights = { -0.5f, -0.1f, 0.2f, 0.5f };
            float[] widths = { 1f, 0.8f, 0.5f, 0f };
            List<Vector3> corners = new List<Vector3>();
            for (int level = 0; level < heights.Length - 1; level++)
            {
                for (int side = 0; side < 4; side++)
                {
                    Vector3 a = LeafCorner(side, heights[level], widths[level]);
                    Vector3 b = LeafCorner(side + 1, heights[level], widths[level]);
                    Vector3 c = LeafCorner(side, heights[level + 1], widths[level + 1]);
                    Vector3 d = LeafCorner(side + 1, heights[level + 1], widths[level + 1]);
                    corners.AddRange(new Vector3[] { a, c, b });
                    if (widths[level + 1] > 0f)
                    {
                        corners.AddRange(new Vector3[] { b, c, d });
                    }
                }
            }

            Vector3[] cap = new Vector3[4];
            for (int side = 0; side < 4; side++)
            {
                cap[side] = LeafCorner(side, heights[0], widths[0]);
            }

            corners.AddRange(new Vector3[] { cap[0], cap[1], cap[2] });
            corners.AddRange(new Vector3[] { cap[0], cap[2], cap[3] });
            return CreateFlatShaded("Leaf", corners);
        }

        static Vector3 LeafCorner(int side, float height, float width)
        {
            float angle = (side % 4) * Mathf.PI * 0.5f;
            return new Vector3(Mathf.Cos(angle) * width * 0.5f, height, Mathf.Sin(angle) * width * 0.5f);
        }

        // One normal per triangle. The shapes are star-shaped around the origin, so a triangle whose normal
        // points at the origin is turned around.
        static Mesh CreateFlatShaded(string name, List<Vector3> corners)
        {
            Vector3[] vertices = new Vector3[corners.Count];
            Vector3[] normals = new Vector3[corners.Count];
            int[] triangles = new int[corners.Count];
            for (int i = 0; i < corners.Count; i += 3)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                Vector3 c = corners[i + 2];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, a + b + c) < 0f)
                {
                    Vector3 swap = b;
                    b = c;
                    c = swap;
                    normal = -normal;
                }

                vertices[i] = a;
                vertices[i + 1] = b;
                vertices[i + 2] = c;
                for (int j = 0; j < 3; j++)
                {
                    normals[i + j] = normal;
                    triangles[i + j] = i + j;
                }
            }

            return PrimitiveMeshBaker.CreateMesh(name, vertices, triangles, normals, null);
        }

        // Flat-shaded octahedron, kept asymmetric on purpose
        public static Mesh CreateBoulder()
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

            return PrimitiveMeshBaker.CreateMesh("Boulder", vertices, triangles);
        }
    }
}
