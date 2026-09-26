using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The bodies' capsules as ground stamps, heights measured from the grass surface
    public static class BodyStamps
    {
        // In cells: the band beside a body where the grass already leans away
        public static readonly float Margin = 0.3f;
        // Radians of lean away from a body at full contact
        public static readonly float Lean = 0.9f;

        public static GroundStamp Create(BodyCapsule capsule, float surfaceY, float cellSize)
        {
            Vector3 lift = new Vector3(0f, surfaceY, 0f);
            float grassHeight = GrassLayout.TuftHeight * cellSize;
            return GroundStamp.Body(capsule.start - lift, capsule.end - lift, capsule.radius, Margin * cellSize,
                                    grassHeight, Lean, 1f);
        }

        // Writes one stamp per capsule from start on, as many as fit, and returns how many it wrote
        public static int Append(BodyCapsule[] capsules, int count, float surfaceY, float cellSize,
                                 GroundStamp[] into, int start)
        {
            if (capsules == null || into == null)
            {
                return 0;
            }

            int written = 0;
            for (int i = 0; i < count && i < capsules.Length && start + written < into.Length; i++)
            {
                into[start + written] = Create(capsules[i], surfaceY, cellSize);
                written++;
            }

            return written;
        }
    }
}
