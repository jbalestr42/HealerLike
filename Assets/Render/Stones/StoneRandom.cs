namespace HealerLike.Render.Stones
{
    // Version 1: full-period uint LCG where zero is a valid seed, its high 24 bits convert to float exactly
    public struct StoneRandom
    {
        uint _state;

        public StoneRandom(uint seed)
        {
            _state = seed;
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
