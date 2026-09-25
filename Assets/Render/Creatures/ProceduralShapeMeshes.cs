using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Closed solids in a centered unit box. Size belongs to the recipe; these parameters change the profile.
    public static class ProceduralShapeMeshes
    {
        public static Mesh Create(ShapeProfile shape, int variant = 0)
        {
            if (!shape.isProcedural || !shape.IsValid())
            {
                return null;
            }

            Geometry(shape, variant, out List<Vector3> vertices, out List<int> indices);
            bool mineral = shape.kind == ShapeKind.Block || shape.kind == ShapeKind.Shard
                || (shape.kind == ShapeKind.Ring && shape.faceted);
            return CreateMesh(shape, variant, vertices, indices, mineral);
        }

        // A curved profile's pole need not be on the box's Y axis. Use the same generated vertices as the mesh,
        // before flat-face splitting, so an attachment follows edits without creating a temporary Unity object.
        public static Vector3 Anchor(ShapeProfile shape, ShapeAnchor anchor, int variant = 0)
        {
            if (anchor < ShapeAnchor.Center || anchor > ShapeAnchor.Top)
            {
                throw new ArgumentOutOfRangeException(nameof(anchor));
            }
            if (!shape.IsValid())
            {
                throw new ArgumentException("Invalid shape profile.", nameof(shape));
            }
            if (anchor == ShapeAnchor.Center)
            {
                return Vector3.zero;
            }
            if (!shape.isProcedural)
            {
                return Vector3.up * (anchor == ShapeAnchor.Top ? 0.5f : -0.5f);
            }

            Geometry(shape, variant, out List<Vector3> vertices, out _);
            bool top = anchor == ShapeAnchor.Top;
            float extreme = top ? float.MinValue : float.MaxValue;
            foreach (Vector3 point in vertices)
            {
                extreme = top ? Mathf.Max(extreme, point.y) : Mathf.Min(extreme, point.y);
            }
            Vector3 total = Vector3.zero;
            int count = 0;
            foreach (Vector3 point in vertices)
            {
                if (Mathf.Abs(point.y - extreme) <= 0.000001f)
                {
                    total += point;
                    count++;
                }
            }
            return total / count;
        }

        static void Geometry(ShapeProfile shape, int variant, out List<Vector3> vertices, out List<int> indices)
        {
            vertices = new List<Vector3>();
            indices = new List<int>();
            if (shape.kind == ShapeKind.Ring)
            {
                Ring(shape, vertices, indices);
            }
            else if (shape.kind == ShapeKind.Block || shape.kind == ShapeKind.Shard)
            {
                Block(shape, variant, vertices, indices);
            }
            else
            {
                Growth(shape, vertices, indices);
            }
            Normalize(vertices);
        }

        static void Growth(ShapeProfile shape, List<Vector3> vertices, List<int> indices)
        {
            int[] previous = null;
            for (int j = 0; j <= shape.lengthSegments; j++)
            {
                float t = (float)j / shape.lengthSegments;
                bool end = j == 0 || j == shape.lengthSegments;
                bool pole = end && shape.kind != ShapeKind.Segment;
                float swell = Mathf.Sin(Mathf.PI * t);
                // Y advances linearly, so a round bulb needs a circular cross-section, not a sine spindle.
                float profile = shape.kind == ShapeKind.Bulb
                    ? Mathf.Sqrt(Mathf.Max(0f, 1f - (2f * t - 1f) * (2f * t - 1f))) : swell;
                float radius = shape.kind == ShapeKind.Segment
                    ? 0.5f * (0.55f + shape.fullness * swell)
                    : 0.5f * Mathf.Pow(Mathf.Max(0f, profile), shape.fullness);
                radius *= 1f + shape.taper * (1f - 2f * t);
                Vector3 centre = new Vector3(shape.bend * t * t, t - 0.5f, 0f);
                int[] ring = new int[pole ? 1 : shape.radialSegments];
                for (int i = 0; i < ring.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / shape.radialSegments;
                    ring[i] = vertices.Count;
                    vertices.Add(centre + (pole ? Vector3.zero
                        : new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius)));
                }
                if (previous == null && !pole)
                {
                    Cap(ring, centre, false, vertices, indices);
                }
                if (previous != null)
                {
                    Join(previous, ring, indices);
                }
                if (j == shape.lengthSegments && !pole)
                {
                    Cap(ring, centre, true, vertices, indices);
                }
                previous = ring;
            }
        }

        // Four clipped rectangular rings make broad faces and distinct bevels, rather than a noisy sphere.
        static void Block(ShapeProfile shape, int variant, List<Vector3> vertices, List<int> indices)
        {
            SeededRandom random = new SeededRandom(unchecked((uint)variant) ^ 0x786431u);
            float skewX = random.Range(-shape.asymmetry, shape.asymmetry);
            float skewZ = random.Range(-shape.asymmetry, shape.asymmetry);
            float[] heights = { -0.5f, -0.5f + shape.bevel, 0.5f - shape.bevel, 0.5f };
            float cut = 0.5f - shape.bevel;
            Vector2[] corners =
            {
                new Vector2(0.5f, -cut), new Vector2(0.5f, cut),
                new Vector2(cut, 0.5f), new Vector2(-cut, 0.5f),
                new Vector2(-0.5f, cut), new Vector2(-0.5f, -cut),
                new Vector2(-cut, -0.5f), new Vector2(cut, -0.5f)
            };
            int[] previous = null;
            for (int j = 0; j < heights.Length; j++)
            {
                float y = heights[j];
                float t = y + 0.5f;
                float endBevel = j == 0 || j == heights.Length - 1 ? 1f - shape.bevel : 1f;
                float width = endBevel * (1f - shape.taper * t);
                Vector3 centre = new Vector3(shape.bend * t * t + skewX * y, y, skewZ * y);
                int[] ring = new int[corners.Length];
                for (int i = 0; i < corners.Length; i++)
                {
                    ring[i] = vertices.Count;
                    vertices.Add(centre + new Vector3(corners[i].x * width, 0f, corners[i].y * width));
                }
                if (previous == null)
                {
                    Cap(ring, centre, false, vertices, indices);
                }
                else
                {
                    Join(previous, ring, indices);
                }
                if (j == heights.Length - 1)
                {
                    Cap(ring, centre, true, vertices, indices);
                }
                previous = ring;
            }
        }

        static void Ring(ShapeProfile shape, List<Vector3> vertices, List<int> indices)
        {
            float minor = shape.tubeRatio * 0.5f;
            float major = 0.5f - minor;
            for (int i = 0; i < shape.radialSegments; i++)
            {
                float theta = i * Mathf.PI * 2f / shape.radialSegments;
                Vector3 radial = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
                for (int j = 0; j < shape.lengthSegments; j++)
                {
                    float phi = j * Mathf.PI * 2f / shape.lengthSegments;
                    vertices.Add(radial * (major + minor * Mathf.Cos(phi)) + Vector3.up * (minor * Mathf.Sin(phi)));
                }
            }
            for (int i = 0; i < shape.radialSegments; i++)
            {
                for (int j = 0; j < shape.lengthSegments; j++)
                {
                    int a = i * shape.lengthSegments + j;
                    int b = ((i + 1) % shape.radialSegments) * shape.lengthSegments + j;
                    int c = i * shape.lengthSegments + (j + 1) % shape.lengthSegments;
                    int d = ((i + 1) % shape.radialSegments) * shape.lengthSegments + (j + 1) % shape.lengthSegments;
                    Triangle(a, c, b, indices);
                    Triangle(b, c, d, indices);
                }
            }
        }

        static void Join(int[] lower, int[] upper, List<int> indices)
        {
            int count = Mathf.Max(lower.Length, upper.Length);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                if (lower.Length == 1)
                {
                    Triangle(lower[0], upper[i], upper[next], indices);
                }
                else if (upper.Length == 1)
                {
                    Triangle(lower[i], upper[0], lower[next], indices);
                }
                else
                {
                    Triangle(lower[i], upper[i], lower[next], indices);
                    Triangle(lower[next], upper[i], upper[next], indices);
                }
            }
        }

        static void Cap(int[] ring, Vector3 centre, bool top, List<Vector3> vertices, List<int> indices)
        {
            int middle = vertices.Count;
            vertices.Add(centre);
            for (int i = 0; i < ring.Length; i++)
            {
                int next = (i + 1) % ring.Length;
                Triangle(middle, ring[top ? next : i], ring[top ? i : next], indices);
            }
        }

        static void Triangle(int a, int b, int c, List<int> indices)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        static void Normalize(List<Vector3> vertices)
        {
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 point in vertices)
            {
                bounds.Encapsulate(point);
            }
            Vector3 size = bounds.size;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 p = vertices[i] - bounds.center;
                vertices[i] = new Vector3(p.x / size.x, p.y / size.y, p.z / size.z);
            }
        }

        static Mesh CreateMesh(ShapeProfile shape, int variant, List<Vector3> points, List<int> indices, bool mineral)
        {
            Mesh mesh = new Mesh { name = "Procedural" + shape.kind, hideFlags = HideFlags.DontSave };
            bool flat = mineral || shape.faceted;
            if (!flat)
            {
                mesh.SetVertices(points);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
            }
            else
            {
                Vector3[] smooth = new Vector3[points.Count];
                Vector3[] vertices = new Vector3[indices.Count];
                Vector3[] normals = new Vector3[indices.Count];
                List<Vector3> outline = new List<Vector3>(indices.Count);
                List<int> body = new List<int>();
                List<int> ochre = new List<int>();
                for (int face = 0; face < indices.Count; face += 3)
                {
                    Vector3 a = points[indices[face]];
                    Vector3 b = points[indices[face + 1]];
                    Vector3 c = points[indices[face + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    // A few complete adjacent triangle pairs retain the existing stone palette treatment.
                    bool warm = mineral && ((face / 6 + (variant & 7)) % 7 == 1);
                    for (int j = 0; j < 3; j++)
                    {
                        int index = face + j;
                        vertices[index] = points[indices[index]];
                        normals[index] = normal;
                        smooth[indices[index]] += normal;
                        (warm ? ochre : body).Add(index);
                    }
                }
                foreach (int index in indices)
                {
                    outline.Add(smooth[index].normalized);
                }
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.SetUVs(3, outline);
                mesh.subMeshCount = mineral ? 2 : 1;
                mesh.SetTriangles(body, 0);
                if (mineral)
                {
                    mesh.SetTriangles(ochre, 1);
                }
            }
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
