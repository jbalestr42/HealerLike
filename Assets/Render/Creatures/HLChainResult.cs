using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLChainResult
    {
        public readonly bool reached;
        public readonly bool clamped;
        public readonly Vector3 effectiveTarget;
        public readonly float error;
        public readonly int iterations;

        public HLChainResult(bool reached, bool clamped, Vector3 target, float error, int iterations)
        {
            this.reached = reached;
            this.clamped = clamped;
            effectiveTarget = target;
            this.error = error;
            this.iterations = iterations;
        }
    }
}
