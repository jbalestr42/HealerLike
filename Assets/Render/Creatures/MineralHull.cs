using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Six broad faces clipped by twelve unequal edge planes. Every cut remains a convex, planar polygon.
    public static class MineralHull
    {
        class Face
        {
            public List<Vector3> points;
            public int id;

            public Face(List<Vector3> points, int id = -1)
            {
                this.points = points;
                this.id = id;
            }
        }

        struct Cut
        {
            public Vector3 normal;
            public float distance;

            public Cut(Vector3 normal, float distance)
            {
                float length = normal.magnitude;
                this.normal = normal / length;
                this.distance = distance / length;
            }
        }

        public static void Generate(
            ShapeProfile shape,
            int variant,
            List<Vector3> vertices,
            List<int> indices,
            List<int> facets
        )
        {
            SeededRandom random = new SeededRandom(unchecked((uint)variant) ^ 0x796432u);
            float fracture = shape.fracture;
            float slope = shape.taper * 0.5f;
            float sideDistance = 0.5f - shape.taper * 0.25f;
            Cut[] sides = new Cut[6];
            for (int i = 0; i < 4; i++)
            {
                Vector3 normal = i < 2 ? Vector3.right * (i == 0 ? -1f : 1f) : Vector3.forward * (i == 2 ? -1f : 1f);
                normal.y = slope + random.Range(-0.13f, 0.13f) * fracture;
                if (i < 2)
                {
                    normal.z = random.Range(-0.1f, 0.1f) * fracture;
                }
                else
                {
                    normal.x = random.Range(-0.1f, 0.1f) * fracture;
                }

                sides[i] = new Cut(normal, sideDistance + random.Range(-0.035f, 0.035f) * fracture);
            }

            sides[4] = new Cut(Vector3.down, 0.5f);
            float crownAngle = random.Range(0f, Mathf.PI * 2f);
            float crownSlope = random.Range(0.22f, 0.42f) * fracture;
            sides[5] = new Cut(
                new Vector3(Mathf.Cos(crownAngle) * crownSlope, 1f, Mathf.Sin(crownAngle) * crownSlope),
                0.5f
            );
            List<Face> faces = Box();
            for (int i = 0; i < sides.Length; i++)
            {
                faces = Clip(faces, sides[i], i);
            }

            // A point retained by every clipping plane keeps the solid nonempty even at extreme bevel/taper.
            Vector3 interior = new Vector3(0f, -0.2f, 0f);
            int cutId = sides.Length;
            for (int a = 0; a < sides.Length; a++)
            {
                for (int b = a + 1; b < sides.Length; b++)
                {
                    if (a / 2 == b / 2)
                    {
                        continue;
                    }

                    float balance = random.Range(-0.48f, 0.48f) * fracture;
                    Vector3 combined = sides[a].normal * (1f + balance) + sides[b].normal * (1f - balance);
                    float distance = sides[a].distance * (1f + balance) + sides[b].distance * (1f - balance);
                    Cut cut = new Cut(combined, distance);
                    float amount = shape.bevel * 0.65f * (1f + random.Range(-0.65f, 0.85f) * fracture);
                    float clearance = cut.distance - Vector3.Dot(cut.normal, interior);
                    cut.distance -= Mathf.Min(amount, clearance * 0.55f);
                    faces = Clip(faces, cut, cutId++);
                }
            }

            // A few large planes give a slab a mineral ridge, rather than adding small surface triangles.
            // Use the same seed for a restrained diagonal lean; the other cuts and the cap stay intact.
            if (shape.ridge > 0f)
            {
                for (int side = 2; side <= 3; side++)
                {
                    float lean = random.Range(-0.35f, 0.35f);
                    Vector3 across = new Vector3(1f, lean, 0f).normalized * shape.ridge;
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Cut ridge = new Cut(
                            sides[side].normal + across * sign,
                            sides[side].distance - 0.015f * shape.ridge
                        );
                        float interiorDistance = Vector3.Dot(ridge.normal, interior);
                        ridge.distance = Mathf.Max(ridge.distance, interiorDistance + 0.08f);
                        faces = Clip(faces, ridge, cutId++);
                    }
                }
            }

            for (int face = 0; face < faces.Count; face++)
            {
                List<Vector3> polygon = faces[face].points;
                int first = Weld(vertices, polygon[0]);
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    indices.Add(first);
                    indices.Add(Weld(vertices, polygon[i]));
                    indices.Add(Weld(vertices, polygon[i + 1]));
                    facets.Add(faces[face].id);
                }
            }

            float skewX = random.Range(-shape.asymmetry, shape.asymmetry);
            float skewZ = random.Range(-shape.asymmetry, shape.asymmetry);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 point = vertices[i];
                // Affine shears preserve every broad cut plane and the convex hull.
                point.x += shape.bend * (point.y + 0.5f) + skewX * point.y;
                point.z += skewZ * point.y;
                vertices[i] = point;
            }
        }

        static List<Face> Box()
        {
            Vector3[] p =
            {
                new Vector3(-2f, -2f, -2f),
                new Vector3(2f, -2f, -2f),
                new Vector3(2f, 2f, -2f),
                new Vector3(-2f, 2f, -2f),
                new Vector3(-2f, -2f, 2f),
                new Vector3(2f, -2f, 2f),
                new Vector3(2f, 2f, 2f),
                new Vector3(-2f, 2f, 2f),
            };
            return new List<Face>
            {
                new Face(new List<Vector3> { p[0], p[3], p[2], p[1] }),
                new Face(new List<Vector3> { p[4], p[5], p[6], p[7] }),
                new Face(new List<Vector3> { p[0], p[4], p[7], p[3] }),
                new Face(new List<Vector3> { p[1], p[2], p[6], p[5] }),
                new Face(new List<Vector3> { p[0], p[1], p[5], p[4] }),
                new Face(new List<Vector3> { p[3], p[7], p[6], p[2] }),
            };
        }

        static List<Face> Clip(List<Face> faces, Cut cut, int cutId)
        {
            List<Face> result = new List<Face>();
            List<Vector3> rim = new List<Vector3>();
            foreach (Face face in faces)
            {
                List<Vector3> polygon = new List<Vector3>();
                for (int i = 0; i < face.points.Count; i++)
                {
                    Vector3 a = face.points[i];
                    Vector3 b = face.points[(i + 1) % face.points.Count];
                    float da = Vector3.Dot(cut.normal, a) - cut.distance;
                    float db = Vector3.Dot(cut.normal, b) - cut.distance;
                    bool aInside = da <= 0f;
                    bool bInside = db <= 0f;
                    if (aInside)
                    {
                        AddUnique(polygon, a);
                    }

                    if (aInside != bInside)
                    {
                        Vector3 crossing = Vector3.LerpUnclamped(a, b, da / (da - db));
                        AddUnique(polygon, crossing);
                        AddUnique(rim, crossing);
                    }
                }

                if (polygon.Count >= 3)
                {
                    result.Add(new Face(polygon, face.id));
                }
            }

            if (rim.Count >= 3)
            {
                Vector3 centre = Vector3.zero;
                foreach (Vector3 point in rim)
                {
                    centre += point;
                }

                centre /= rim.Count;
                Vector3 reference = Mathf.Abs(cut.normal.y) < 0.8f ? Vector3.up : Vector3.right;
                Vector3 u = Vector3.Cross(reference, cut.normal).normalized;
                Vector3 v = Vector3.Cross(cut.normal, u);
                rim.Sort(
                    (a, b) =>
                        Mathf
                            .Atan2(Vector3.Dot(a - centre, v), Vector3.Dot(a - centre, u))
                            .CompareTo(Mathf.Atan2(Vector3.Dot(b - centre, v), Vector3.Dot(b - centre, u)))
                );
                result.Add(new Face(rim, cutId));
            }

            return result;
        }

        static void AddUnique(List<Vector3> points, Vector3 point)
        {
            foreach (Vector3 existing in points)
            {
                if ((existing - point).sqrMagnitude < 0.000000000001f)
                {
                    return;
                }
            }

            points.Add(point);
        }

        static int Weld(List<Vector3> points, Vector3 point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if ((points[i] - point).sqrMagnitude < 0.000000000001f)
                {
                    return i;
                }
            }

            points.Add(point);
            return points.Count - 1;
        }
    }
}
