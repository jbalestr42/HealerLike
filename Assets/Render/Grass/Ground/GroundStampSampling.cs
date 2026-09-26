using UnityEngine;

namespace HealerLike.Render.Grass
{
    // CPU sampling of the ground wire format, mirroring GroundCommon.hlsl.
    public static class GroundStampSampling
    {
        // The square the stamp can write into: its centre in world XZ and half its side
        public static void Bounds(GroundStamp stamp, out Vector2 centre, out float reach)
        {
            if (stamp.kind == GroundStampKind.Streak)
            {
                Vector2 start = new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
                Vector2 end = new Vector2(stamp.response.x, stamp.response.y);
                centre = 0.5f * (start + end);
                reach = 0.5f * Vector2.Distance(start, end) + stamp.centreRadius.z + stamp.centreRadius.w;
                return;
            }

            if (stamp.kind == GroundStampKind.Body)
            {
                Vector2 start = new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
                Vector2 end = new Vector2(stamp.push.x, stamp.push.y);
                centre = 0.5f * (start + end);
                reach = 0.5f * Vector2.Distance(start, end) + stamp.centreRadius.w + stamp.push.w;
                return;
            }

            centre = new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
            reach = stamp.centreRadius.z + stamp.centreRadius.w;
        }

        // xy lean in radians and z flatness this stamp adds at a world XZ point
        public static Vector3 Sample(GroundStamp stamp, Vector2 point)
        {
            if (stamp.kind == GroundStampKind.Body)
            {
                return SampleBody(stamp, point);
            }

            if (stamp.kind == GroundStampKind.Aura || stamp.kind == GroundStampKind.Streak)
            {
                return Vector3.zero;
            }

            Vector2 delta = point - new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
            float distance = delta.magnitude;
            Vector2 outward = distance > 1e-5f ? delta / distance : Vector2.zero;
            float radius = stamp.centreRadius.z;
            float band = stamp.centreRadius.w;
            float weight;
            if (stamp.kind == GroundStampKind.Front)
            {
                weight = 1f - SmoothStep(0f, band, Mathf.Abs(distance - radius));
            }
            else
            {
                float rim = radius * (1f - stamp.shape.z
                    * (0.5f + 0.5f * Mathf.Sin(Vector2.Dot(point, GroundStamp.RimFrequency))));
                weight = 1f - SmoothStep(rim * (1f - stamp.shape.y), rim, distance);
            }

            Vector2 lean = stamp.kind == GroundStampKind.Front ? outward * (stamp.push.w * weight)
                                                          : Turn(outward, stamp.push.x) * (stamp.push.w * weight);

            return new Vector3(lean.x, lean.y, stamp.shape.x * weight);
        }

        // What an aura asks of the ground state at a world XZ point, scaled by its cover there: x ash, y vitality,
        // z light, w blight. Overlapping auras add up. Zero for every other kind.
        public static Vector4 State(GroundStamp stamp, Vector2 point)
        {
            if (stamp.kind == GroundStampKind.Streak)
            {
                return stamp.push * StreakWeight(stamp, point);
            }

            if (stamp.kind != GroundStampKind.Aura)
            {
                return Vector4.zero;
            }

            return stamp.push * DiscWeight(stamp, point);
        }

        // HLGroundStreakWeight: the distance to the zigzag path through the segment, thinning toward its end
        static float StreakWeight(GroundStamp stamp, Vector2 point)
        {
            Vector2 start = new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
            Vector2 axis = new Vector2(stamp.response.x, stamp.response.y) - start;
            float length = Mathf.Max(axis.magnitude, 1e-5f);
            Vector2 along = axis / length;
            Vector2 delta = point - start;
            float t = Mathf.Clamp01(Vector2.Dot(delta, along) / length);
            float side = along.x * delta.y - along.y * delta.x;
            float turn = t * length * stamp.shape.z;
            float zigzag = 1f - 4f * Mathf.Abs(Frac(turn + 0.25f) - 0.5f);
            float slope = 4f * stamp.centreRadius.w * stamp.shape.z;
            float offset = Mathf.Abs(side - stamp.centreRadius.w * zigzag) / Mathf.Sqrt(1f + slope * slope);
            float beyond = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(delta, along) - t * length));
            float distance = Mathf.Sqrt(offset * offset + beyond * beyond);
            float width = stamp.centreRadius.z * (1f - 0.5f * t);
            return 1f - SmoothStep(width * (1f - stamp.shape.y), width, distance);
        }

        static float DiscWeight(GroundStamp stamp, Vector2 point)
        {
            float distance = Vector2.Distance(point, new Vector2(stamp.centreRadius.x, stamp.centreRadius.y));
            float radius = stamp.centreRadius.z;
            float rim = radius * (1f - stamp.shape.z
                    * (0.5f + 0.5f * Mathf.Sin(Vector2.Dot(point, GroundStamp.RimFrequency))));
            return 1f - SmoothStep(rim * (1f - stamp.shape.y), rim, distance);
        }

        // The lean the stamp holds and the flatness it asks for at a world XZ point
        public static Vector3 Target(GroundStamp stamp, Vector2 point)
        {
            Vector3 value = Sample(stamp, point);
            return new Vector3(value.x * stamp.response.z, value.y * stamp.response.z, value.z);
        }

        // The acceleration the stamp throws the grass with at a world XZ point
        public static Vector2 Force(GroundStamp stamp, Vector2 point)
        {
            Vector3 value = Sample(stamp, point);
            return new Vector2(value.x, value.y) * stamp.response.w;
        }

        // Rotates v counterclockwise seen from above, from +X toward +Z, by angle radians
        public static Vector2 Turn(Vector2 v, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // HLGroundBodyValue: the nearest point of the capsule's ground shadow, how low the body sits over it, and
        // how far out the grass still feels it
        static Vector3 SampleBody(GroundStamp stamp, Vector2 point)
        {
            Vector2 start = new Vector2(stamp.centreRadius.x, stamp.centreRadius.y);
            Vector2 end = new Vector2(stamp.push.x, stamp.push.y);
            Vector2 axis = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, axis) / Mathf.Max(Vector2.Dot(axis, axis), 1e-6f));
            Vector2 nearest = start + axis * t;
            float height = Mathf.Lerp(stamp.centreRadius.z, stamp.push.z, t);
            float radius = stamp.centreRadius.w;
            Vector2 delta = point - nearest;
            float distance = delta.magnitude;
            Vector2 outward = distance > 1e-5f ? delta / distance : Vector2.zero;
            float underside = height - Mathf.Sqrt(Mathf.Max(radius * radius - distance * distance, 0f));
            float contact = Mathf.Clamp01((stamp.response.x - underside) / stamp.response.x);
            float near = 1f - SmoothStep(radius, radius + stamp.push.w, distance);
            float covered = 1f - SmoothStep(0.6f * radius, Mathf.Max(radius, 1e-4f), distance);
            Vector2 lean = outward * (stamp.response.y * contact * near);
            return new Vector3(lean.x, lean.y, stamp.shape.x * contact * covered);
        }

        // HLSL frac
        static float Frac(float x)
        {
            return x - Mathf.Floor(x);
        }

        // HLSL smoothstep, which Mathf.SmoothStep is not
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0)
            {
                return x < edge0 ? 0f : 1f;
            }

            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
