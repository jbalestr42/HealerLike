namespace HealerLike.Render.Stones
{
    // Version 1: full-period uint LCG. Zero is a valid seed. Use the high 24 bits for exact float conversion.
    public struct HLStoneRandom
    {
        uint state;
        public HLStoneRandom(uint seed) { state = seed; }
        public uint Next() { state = unchecked(state * 1664525u + 1013904223u); return state; }
        public float Next01() => (Next() >> 8) * (1f / 16777216f);
        public float Range(float min, float max) => min + (max-min) * Next01();
    }
}
