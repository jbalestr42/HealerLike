using UnityEngine;

namespace HealerLike.Render
{
    // Stable cosmetic variation, without consuming the gameplay random stream.
    // Both keep the hue within 6 degrees and the value within 8%; each hash is pinned by what the sheets show.
    public static class ColourJitter
    {
        static readonly float hueDegrees = 6f;
        static readonly Vector2 valueScale = new Vector2(0.92f, 1.08f);

        // A unit's parts, from its idle seed
        public static Color Vary(Color colour, int seed)
        {
            uint hash = (uint)seed * 747796405u + 2891336453u;
            hash = ((hash >> (int)((hash >> 28) + 4)) ^ hash) * 277803737u;
            hash = (hash >> 22) ^ hash;
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            float hue = ((hash & 65535) / 65535f * 2f - 1f) / 60f;
            float value = ((hash >> 16) / 65535f * 2f - 1f) * 0.08f;
            Color result = Color.HSVToRGB(Mathf.Repeat(h + hue, 1f), s, v * (1f + value), true);
            result.a = colour.a;
            return result;
        }

        // A scenery item, from the scatter's colour seed
        public static Color VaryScenery(Color colour, uint seed)
        {
            SeededRandom random = new SeededRandom(seed);
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            float hue = Mathf.Repeat(h + random.Range(-hueDegrees, hueDegrees) / 360f, 1f);
            float value = Mathf.Clamp01(v * random.Range(valueScale.x, valueScale.y));
            return Color.HSVToRGB(hue, s, value);
        }
    }
}
