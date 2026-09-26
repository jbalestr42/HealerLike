using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // One shape drawn additively into the ground target each frame: a disc or a travelling ring front that asks
    // the grass under it to lean, outward from its centre or along a heading, and to lie flat. HLGroundStamp in
    // GroundCommon.hlsl is the GPU side and Sample mirrors HLGroundStampValue.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GroundStamp
    {
        // Bytes per element of the stamp buffer, the marshalled size of the fields below
        public static readonly int Stride = 48;
        // The rim wobble's spatial frequency, HL_GROUND_RIM_FREQUENCY in GroundCommon.hlsl
        public static readonly Vector2 RimFrequency = new Vector2(9.7f, 6.3f);

        // xy centre in world XZ, z radius, w half width of a ring front, zero for a disc
        public Vector4 centreRadius;
        // xy heading, z lean along the heading in radians, w lean away from the centre in radians
        public Vector4 push;
        // x how flat the grass lies, y the share of the radius the disc edge fades over, z the share of the
        // radius the rim wobbles inward by, w one to act only ahead of the centre along the heading
        public Vector4 shape;

        // A disc with a soft edge; wobble lets its rim wander inward so it never reads as a stamped circle
        public static GroundStamp Disc(Vector2 centre, float radius, float outward, float crush, float edgeShare,
                                       float wobble)
        {
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), 0f),
                push = new Vector4(0f, 0f, 0f, outward),
                shape = new Vector4(crush, Mathf.Clamp(edgeShare, 0.01f, 1f), Mathf.Clamp01(wobble), 0f)
            };
        }

        // A ring front at radius, band wide either side, pushing along heading ahead of the centre only
        public static GroundStamp Front(Vector2 centre, float radius, float band, Vector2 heading, float lean)
        {
            Vector2 direction = heading.sqrMagnitude > 1e-10f ? heading.normalized : Vector2.zero;
            return new GroundStamp
            {
                centreRadius = new Vector4(centre.x, centre.y, Mathf.Max(0f, radius), Mathf.Max(0.001f, band)),
                push = new Vector4(direction.x, direction.y, lean, 0f),
                shape = new Vector4(0f, 1f, 0f, 1f)
            };
        }

        // How far from its centre the stamp can write, the half size of the quad that draws it
        public float reach
        {
            get { return centreRadius.z + centreRadius.w; }
        }

        // xy lean in radians and z flatness this stamp adds at a world XZ point
        public Vector3 Sample(Vector2 point)
        {
            Vector2 delta = point - new Vector2(centreRadius.x, centreRadius.y);
            float distance = delta.magnitude;
            Vector2 outward = distance > 1e-5f ? delta / distance : Vector2.zero;
            float radius = centreRadius.z;
            float band = centreRadius.w;
            float weight;
            if (band > 0f)
            {
                weight = 1f - SmoothStep(0f, band, Mathf.Abs(distance - radius));
            }
            else
            {
                float rim = radius * (1f - shape.z * (0.5f + 0.5f * Mathf.Sin(Vector2.Dot(point, RimFrequency))));
                weight = 1f - SmoothStep(rim * (1f - shape.y), rim, distance);
            }

            Vector2 heading = new Vector2(push.x, push.y);
            if (shape.w > 0f)
            {
                weight *= Mathf.Clamp01(Vector2.Dot(outward, heading));
            }

            Vector2 lean = (heading * push.z + outward * push.w) * weight;
            return new Vector3(lean.x, lean.y, shape.x * weight);
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
