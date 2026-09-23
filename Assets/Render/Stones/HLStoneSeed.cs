using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class HLStoneSeed
    {
        public static uint ForCell(int gridSeed, Vector2Int coord)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = ForPart(hash, (uint)gridSeed);
                hash = ForPart(hash, (uint)coord.x);
                hash = ForPart(hash, (uint)coord.y);
                return ForPart(hash, 1);
            }
        }

        public static uint ForPart(uint seed, uint partSalt)
        {
            return unchecked((seed ^ partSalt) * 16777619);
        }
    }
}
