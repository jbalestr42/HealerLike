using UnityEngine;
namespace HealerLike.Render.Stones
{
    public static class HLStoneSeed
    {
        public static uint ForCell(int gridSeed, Vector2Int coord)
        {
            unchecked
            {
                uint h = 2166136261;
                h = ForPart(h, (uint)gridSeed); h = ForPart(h, (uint)coord.x);
                h = ForPart(h, (uint)coord.y); return ForPart(h, 1);
            }
        }
        public static uint ForPart(uint seed, uint partSalt) => unchecked((seed ^ partSalt) * 16777619);
    }
}
