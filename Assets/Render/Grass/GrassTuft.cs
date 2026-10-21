using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Where a grass tuft vertex lands: the tuft mesh is GrassBladeMesh, the spire of FacetedMeshes.CreatePyramid
    // cut into segments, x and z in tuft widths and y in tuft heights. Place mirrors HLPlaceGrassTuft in
    // GrassInstancing.hlsl. A lean bends the tuft along an arc of its own length: the tangent turns from RootBend
    // of the lean at the root to RootBend plus TipBend at the tip, so the chord from root to tip tilts by the
    // lean itself, as the rigid tuft did, while the spire curls.
    public static class GrassTuft
    {
        public static readonly float RootBend = 0.4f;
        public static readonly float TipBend = 1.2f;

        // Rotates v about the horizontal axis that tips +Y toward lean, by the length of lean in radians
        public static Vector3 Tilt(Vector3 v, Vector2 lean)
        {
            float angle = Mathf.Max(lean.magnitude, 0.00001f);
            Vector3 axis = new Vector3(lean.y, 0f, -lean.x) / angle;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return v * cos + Vector3.Cross(axis, v) * sin + axis * (Vector3.Dot(axis, v) * (1f - cos));
        }

        // The tangent's lean a height share t along the tuft
        public static Vector2 LeanAt(Vector2 lean, float t)
        {
            return lean * (RootBend + TipBend * t);
        }

        // The point a height share t along the bent axis of a tuft this tall
        public static Vector3 Spine(Vector2 lean, float height, float t)
        {
            float angle = lean.magnitude;
            Vector2 heading = angle > 1e-5f ? lean / angle : Vector2.zero;
            float turn = angle * TipBend * t;
            float mean = angle * (RootBend + 0.5f * TipBend * t);
            float chord = height * t * Sinc(0.5f * turn);
            float across = chord * Mathf.Sin(mean);
            return new Vector3(heading.x * across, chord * Mathf.Cos(mean), heading.y * across);
        }

        // Scale and yaw, then bend: the section at each height rides the spine and turns with its tangent
        public static Vector3 Place(Vector3 positionOS, Vector3 root, float yaw, float width, float height,
                                    Vector2 lean)
        {
            float t = positionOS.y;
            Vector3 section = Yaw(new Vector3(positionOS.x * width, 0f, positionOS.z * width), yaw);
            return root + Spine(lean, height, t) + Tilt(section, LeanAt(lean, t));
        }

        // The normal takes the inverse of the scale, then the same yaw and the tangent's turn at its height share
        public static Vector3 PlaceNormal(Vector3 normalOS, float t, float yaw, float width, float height,
                                          Vector2 lean)
        {
            Vector3 scaled = new Vector3(normalOS.x / width, normalOS.y / height, normalOS.z / width);
            return Tilt(Yaw(scaled, yaw), LeanAt(lean, t)).normalized;
        }

        // sin(x) / x, one at zero
        public static float Sinc(float x)
        {
            return Mathf.Abs(x) < 1e-4f ? 1f - x * x / 6f : Mathf.Sin(x) / x;
        }

        // Yaw in radians about +Y, the turn Quaternion.Euler(0, yaw, 0) makes
        static Vector3 Yaw(Vector3 v, float yaw)
        {
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            return new Vector3(v.x * cos + v.z * sin, v.y, v.z * cos - v.x * sin);
        }
    }
}
