using UnityEngine;

namespace HealerLike.Render.Zones
{
    // A chain of joints, a liana, as brushing capsules along its real curve: consecutive joints grouped so the
    // whole chain fits in at most maxSegments capsules, each skipped where it flies above the ceiling
    public static class ChainCapsules
    {
        public static int Append(Vector3[] joints, int jointCount, float radius, float ceiling, BodyCapsule[] into,
                                 int start, int maxSegments)
        {
            if (joints == null || into == null || jointCount < 2 || maxSegments < 1)
            {
                return 0;
            }

            jointCount = Mathf.Min(jointCount, joints.Length);
            int stride = Mathf.Max(1, Mathf.CeilToInt((jointCount - 1) / (float)maxSegments));
            int count = 0;
            for (int i = 0; i + 1 < jointCount && start + count < into.Length; i += stride)
            {
                Vector3 a = joints[i];
                Vector3 b = joints[Mathf.Min(i + stride, jointCount - 1)];
                if (!RenderMath.IsFinite(a) || !RenderMath.IsFinite(b) || Mathf.Min(a.y, b.y) - radius > ceiling)
                {
                    continue;
                }

                into[start + count] = new BodyCapsule { start = a, end = b, radius = radius, press = 0f };
                count++;
            }

            return count;
        }
    }
}
