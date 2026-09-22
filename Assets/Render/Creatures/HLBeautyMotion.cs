using UnityEngine;

namespace HealerLike.Render.Creatures
{
    /// <summary>Stable cosmetic variation without consuming the gameplay random stream.</summary>
    public static class HLBeautyMotion
    {
        public static Color Vary(Color colour, int seed)
        {
            uint hash = unchecked((uint)seed * 747796405u + 2891336453u);
            hash = ((hash >> (int)((hash >> 28) + 4)) ^ hash) * 277803737u;
            hash = (hash >> 22) ^ hash;
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            float hue = ((hash & 65535) / 65535f * 2 - 1) / 60f;
            float value = ((hash >> 16) / 65535f * 2 - 1) * .08f;
            Color result = Color.HSVToRGB(Mathf.Repeat(h + hue, 1), s, v * (1 + value), true);
            result.a = colour.a;
            return result;
        }
    }
}
