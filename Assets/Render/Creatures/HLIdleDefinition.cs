using System;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct HLIdleDefinition
    {
        public float swayDegrees;
        public float swayFrequency;
        public float breathAmount;
        public float breathFrequency;
        public int seed;
    }
}
