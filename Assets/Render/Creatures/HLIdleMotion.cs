using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLIdlePose
    {
        public readonly Quaternion sway;
        public readonly Vector3 bodyScale;
        public readonly float bodyLift;
        public HLIdlePose(Quaternion sway, Vector3 scale, float lift) { this.sway = sway; bodyScale = scale; bodyLift = lift; }
    }
    public static class HLIdleMotion
    {
        public static HLIdlePose Evaluate(in HLIdleDefinition settings, float time)
        {
            float seed = (settings.seed & 65535) * .137f;
            float x = Noise(seed, time * settings.swayFrequency);
            float z = Noise(seed + 41.7f, time * settings.swayFrequency);
            float breath = Noise(seed + 93.2f, time * settings.breathFrequency);
            return new HLIdlePose(Quaternion.Euler(x * settings.swayDegrees, 0, z * settings.swayDegrees),
                Vector3.one * (1 + breath * settings.breathAmount), breath * .008f);
        }
        static float Noise(float seed, float time) => Mathf.Clamp(Mathf.PerlinNoise(seed, time) * 2 - 1, -1, 1);
    }
}
