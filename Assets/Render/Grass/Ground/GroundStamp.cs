using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public enum GroundStampKind
    {
        Disc = 0,
        Front = 1,
        Body = 2,
        Aura = 3
    }

    // One shape drawn additively into the ground each frame: a disc, a ring front travelling outward or along a
    // heading, or a body capsule the grass parts around. Its push is held, a lean the grass springs toward and
    // keeps while the stamp lasts, and kicked, an acceleration that throws the grass and lets it swing back.
    // An aura moves nothing: it asks the slow ground state for ash, vitality and glow over a disc.
    // HLGroundStamp in GroundCommon.hlsl is the GPU side and Sample mirrors HLGroundStampValue.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GroundStamp
    {
        // Bytes per element of the stamp buffer, the marshalled size of the fields below
        public static readonly int Stride = 64;
        // The rim wobble's spatial frequency, HL_GROUND_RIM_FREQUENCY in GroundCommon.hlsl
        public static readonly Vector2 RimFrequency = new Vector2(9.7f, 6.3f);

        // Disc and front: xy centre in world XZ, z radius, w half width of the front band.
        // Body: the capsule's first end, x and z in world XZ, y its height above the ground, w its radius.
        public Vector4 centreRadius;
        // Aura: x the ash it asks for, y the vitality, from dead at -1 to lush at 1, z the glow.
        // Disc: x the turn of its push from straight outward, counterclockwise seen from above in radians, a
        // quarter turn swirls; z unused; w the push in radians.
        // Front: xy heading, zero for a ring pushing outward all round; z push along the heading, w push away from
        // the centre, in radians.
        // Body: the capsule's second end, x and z in world XZ, y its height above the ground, w the width of the
        // band around it where the grass already leans away.
        public Vector4 push;
        // x how flat the grass lies, y the share of the radius a disc edge fades over, z the share of the radius
        // its rim wobbles inward by, w the GroundStampKind
        public Vector4 shape;
        // x the grass height a body presses against, y its lean away at full contact; z the share of the push
        // held as a lean, w the acceleration per radian of push, in radians per second squared
        public Vector4 response;

        public GroundStampKind kind
        {
            get { return (GroundStampKind)Mathf.RoundToInt(shape.w); }
        }

        // A disc with a soft edge; wobble lets its rim wander inward so it never reads as a stamped circle
        public static GroundStamp Disc(Vector2 centre, float radius, float outward, float crush, float edgeShare,
                                       float wobble)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), 0f),
                push = new Vector4(0f, 0f, 0f, outward),
                shape = new Vector4(crush, Mathf.Clamp(edgeShare, 0.01f, 1f), Mathf.Clamp01(wobble),
                                    (float)GroundStampKind.Disc),
                response = new Vector4(0f, 0f, 1f, 0f)
            };
        }

        // A disc that throws the grass around its centre instead of holding it: turn a quarter to spin it
        public static GroundStamp Swirl(Vector2 centre, float radius, float turn, float push, float kick,
                                        float edgeShare)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), 0f),
                push = new Vector4(turn, 0f, 0f, push),
                shape = new Vector4(0f, Mathf.Clamp(edgeShare, 0.01f, 1f), 0f, (float)GroundStampKind.Disc),
                response = new Vector4(0f, 0f, 0f, kick)
            };
        }

        // A ring front at radius, band wide either side, kicking the grass along heading ahead of the centre
        public static GroundStamp Front(Vector2 centre, float radius, float band, Vector2 heading, float push,
                                        float kick)
        {
            Vector2 direction = heading.sqrMagnitude > 1e-10f ? heading.normalized : Vector2.right;
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), Mathf.Max(0.001f, band)),
                push = new Vector4(direction.x, direction.y, push, 0f),
                shape = new Vector4(0f, 1f, 0f, (float)GroundStampKind.Front),
                response = new Vector4(0f, 0f, 0f, kick)
            };
        }

        // A ring front at radius, band wide either side, kicking the grass outward all round: a blast
        public static GroundStamp Shock(Vector2 centre, float radius, float band, float push, float kick)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), Mathf.Max(0.001f, band)),
                push = new Vector4(0f, 0f, 0f, push),
                shape = new Vector4(0f, 1f, 0f, (float)GroundStampKind.Front),
                response = new Vector4(0f, 0f, 0f, kick)
            };
        }

        // A capsule between two points, heights above the ground: the grass under it lies flat, the grass beside
        // it within margin leans away, and grass taller than a hovering body bends only by what it touches
        public static GroundStamp Body(Vector3 start, Vector3 end, float radius, float margin, float grassHeight,
                                       float lean, float crush)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(start.x, start.z, start.y, Mathf.Max(0f, radius)),
                push = new Vector4(end.x, end.z, end.y, Mathf.Max(0.001f, margin)),
                shape = new Vector4(crush, 1f, 0f, (float)GroundStampKind.Body),
                response = new Vector4(Mathf.Max(0.01f, grassHeight), lean, 1f, 0f)
            };
        }

        // A disc asking the ground state for ash, vitality and glow, the rim wobbling inward like a burn's edge
        public static GroundStamp Aura(Vector2 centre, float radius, float edgeShare, float wobble, float ash,
                                       float vitality, float glow)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), 0f),
                push = new Vector4(Mathf.Clamp01(ash), Mathf.Clamp(vitality, -1f, 1f), Mathf.Clamp01(glow), 0f),
                shape = new Vector4(0f, Mathf.Clamp(edgeShare, 0.01f, 1f), Mathf.Clamp01(wobble),
                                    (float)GroundStampKind.Aura)
            };
        }

        // The square the stamp can write into: its centre in world XZ and half its side
        public void Bounds(out Vector2 centre, out float reach)
        {
            if (kind == GroundStampKind.Body)
            {
                Vector2 start = new Vector2(centreRadius.x, centreRadius.y);
                Vector2 end = new Vector2(push.x, push.y);
                centre = 0.5f * (start + end);
                reach = 0.5f * Vector2.Distance(start, end) + centreRadius.w + push.w;
                return;
            }

            centre = new Vector2(centreRadius.x, centreRadius.y);
            reach = centreRadius.z + centreRadius.w;
        }

        // xy lean in radians and z flatness this stamp adds at a world XZ point
        public Vector3 Sample(Vector2 point)
        {
            if (kind == GroundStampKind.Body)
            {
                return SampleBody(point);
            }

            if (kind == GroundStampKind.Aura)
            {
                return Vector3.zero;
            }

            Vector2 delta = point - new Vector2(centreRadius.x, centreRadius.y);
            float distance = delta.magnitude;
            Vector2 outward = distance > 1e-5f ? delta / distance : Vector2.zero;
            float radius = centreRadius.z;
            float band = centreRadius.w;
            float weight;
            if (kind == GroundStampKind.Front)
            {
                weight = 1f - SmoothStep(0f, band, Mathf.Abs(distance - radius));
            }
            else
            {
                float rim = radius * (1f - shape.z * (0.5f + 0.5f * Mathf.Sin(Vector2.Dot(point, RimFrequency))));
                weight = 1f - SmoothStep(rim * (1f - shape.y), rim, distance);
            }

            Vector2 lean;
            if (kind == GroundStampKind.Front)
            {
                Vector2 heading = new Vector2(push.x, push.y);
                // A heading ahead of the centre only; a ring with no push along it blows all round
                if (push.z != 0f)
                {
                    weight *= Mathf.Clamp01(Vector2.Dot(outward, heading));
                }

                lean = (heading * push.z + outward * push.w) * weight;
            }
            else
            {
                lean = Turn(outward, push.x) * (push.w * weight);
            }

            return new Vector3(lean.x, lean.y, shape.x * weight);
        }

        // What an aura asks of the ground state at a world XZ point, weighted by its coverage there: x ash, y
        // vitality, z glow, w the coverage, so overlapping auras average. Zero for every other kind.
        public Vector4 State(Vector2 point)
        {
            if (kind != GroundStampKind.Aura)
            {
                return Vector4.zero;
            }

            float weight = DiscWeight(point);
            return new Vector4(push.x * weight, push.y * weight, push.z * weight, weight);
        }

        float DiscWeight(Vector2 point)
        {
            float distance = Vector2.Distance(point, new Vector2(centreRadius.x, centreRadius.y));
            float radius = centreRadius.z;
            float rim = radius * (1f - shape.z * (0.5f + 0.5f * Mathf.Sin(Vector2.Dot(point, RimFrequency))));
            return 1f - SmoothStep(rim * (1f - shape.y), rim, distance);
        }

        // The lean the stamp holds and the flatness it asks for at a world XZ point
        public Vector3 Target(Vector2 point)
        {
            Vector3 value = Sample(point);
            return new Vector3(value.x * response.z, value.y * response.z, value.z);
        }

        // The acceleration the stamp throws the grass with at a world XZ point
        public Vector2 Force(Vector2 point)
        {
            Vector3 value = Sample(point);
            return new Vector2(value.x, value.y) * response.w;
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
        Vector3 SampleBody(Vector2 point)
        {
            Vector2 start = new Vector2(centreRadius.x, centreRadius.y);
            Vector2 end = new Vector2(push.x, push.y);
            Vector2 axis = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, axis) / Mathf.Max(Vector2.Dot(axis, axis), 1e-6f));
            Vector2 nearest = start + axis * t;
            float height = Mathf.Lerp(centreRadius.z, push.z, t);
            float radius = centreRadius.w;
            Vector2 delta = point - nearest;
            float distance = delta.magnitude;
            Vector2 outward = distance > 1e-5f ? delta / distance : Vector2.zero;
            float underside = height - Mathf.Sqrt(Mathf.Max(radius * radius - distance * distance, 0f));
            float contact = Mathf.Clamp01((response.x - underside) / response.x);
            float near = 1f - SmoothStep(radius, radius + push.w, distance);
            float covered = 1f - SmoothStep(0.6f * radius, radius, distance);
            Vector2 lean = outward * (response.y * contact * near);
            return new Vector3(lean.x, lean.y, shape.x * contact * covered);
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
