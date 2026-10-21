using System;

namespace HealerLike.Render.Stones
{
    [Serializable]
    public struct StoneSettings
    {
        public float size;
        public float elongation;
        public float depthRatio;
        public float roughness;
        public int subdivisions;
    }
}
