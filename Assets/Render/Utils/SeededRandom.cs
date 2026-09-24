namespace HealerLike.Render
{
    // The layer's seeded random, so a layout, a stone or a colour comes out the same for the same seed.
    // Version 1: full-period uint LCG where zero is a valid seed, its high 24 bits convert to float exactly
    public struct SeededRandom
    {
        uint _state;

        public SeededRandom(uint seed)
        {
            _state = seed;
        }

        // FNV fold of a seed and a salt, one seed per part, item or hit from one base seed
        public static uint ForPart(uint seed, uint partSalt)
        {
            return (seed ^ partSalt) * 16777619;
        }

        public uint Next()
        {
            _state = _state * 1664525u + 1013904223u;
            return _state;
        }

        public float Next01()
        {
            return (Next() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }
    }
}
