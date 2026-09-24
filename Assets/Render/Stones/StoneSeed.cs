namespace HealerLike.Render.Stones
{
    public static class StoneSeed
    {
        public static uint ForPart(uint seed, uint partSalt)
        {
            return (seed ^ partSalt) * 16777619;
        }
    }
}
