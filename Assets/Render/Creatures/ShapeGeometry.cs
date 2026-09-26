using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Normalized topology shared by mesh output and attachment measurement.
    public static class ShapeGeometry
    {
        public static void Build(
            ShapeProfile shape,
            int variant,
            out List<Vector3> vertices,
            out List<int> indices,
            out List<int> facets
        )
        {
            vertices = new List<Vector3>();
            indices = new List<int>();
            facets = null;
            if (shape.kind == ShapeKind.Ring)
            {
                Ring(shape, vertices, indices);
            }
            else if (shape.kind == ShapeKind.Block || shape.kind == ShapeKind.Shard)
            {
                if (shape.fracture > 0f || shape.ridge > 0f)
                {
                    facets = new List<int>();
                    MineralHull.Generate(shape, variant, vertices, indices, facets);
                }
                else
                {
                    Block(shape, variant, vertices, indices);
                }
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
                float profile =
                    shape.kind == ShapeKind.Bulb
                        ? Mathf.Sqrt(Mathf.Max(0f, 1f - (2f * t - 1f) * (2f * t - 1f)))
                        : swell;
                float radius =
                    shape.kind == ShapeKind.Segment
                        ? 0.5f * (0.55f + shape.fullness * swell)
                        : 0.5f * Mathf.Pow(Mathf.Max(0f, profile), shape.fullness);
                radius *= 1f + shape.taper * (1f - 2f * t);
                Vector3 centre = new Vector3(shape.bend * t * t + shape.bow * swell, t - 0.5f, 0f);
                int[] ring = new int[pole ? 1 : shape.radialSegments];
                for (int i = 0; i < ring.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / shape.radialSegments;
                    ring[i] = vertices.Count;
                    vertices.Add(
                        centre
                            + (
                                pole
                                    ? Vector3.zero
                                    : new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius)
                            )
                    );
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
                new Vector2(0.5f, -cut),
                new Vector2(0.5f, cut),
                new Vector2(cut, 0.5f),
                new Vector2(-cut, 0.5f),
                new Vector2(-0.5f, cut),
                new Vector2(-0.5f, -cut),
                new Vector2(-cut, -0.5f),
                new Vector2(cut, -0.5f),
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
    }
}
