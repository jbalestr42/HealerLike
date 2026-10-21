using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class IdleMotion
    {
        public static IdlePose Evaluate(IdleDefinition settings, float time)
        {
            float seed = (settings.seed & 65535) * 0.137f;
            float x = Noise(seed, time * settings.swayFrequency);
            float z = Noise(seed + 41.7f, time * settings.swayFrequency);
            float breath = Noise(seed + 93.2f, time * settings.breathFrequency);
            Quaternion sway = Quaternion.Euler(x * settings.swayDegrees, 0f, z * settings.swayDegrees);
            Vector3 bodyScale = Vector3.one * (1f + breath * settings.breathAmount);
            return new IdlePose(sway, bodyScale);
        }

        static float Noise(float seed, float time)
        {
            return Mathf.Clamp(Mathf.PerlinNoise(seed, time) * 2f - 1f, -1f, 1f);
        }
    }
}
