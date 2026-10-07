using System;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // Presentation only: these values never alter the projectile's position or collision clock.
    [Serializable]
    public class DeliveryPresentation
    {
        [Min(0.1f)] public float size = 1f;
        [Range(0f, 0.25f)] public float pulseAmount;
        [Min(0f)] public float pulseFrequency = 2f;
        [Min(0f)] public float trailSeconds;
        [Range(0f, 1f)] public float trailWidth = 0.35f;
        [Min(0.01f)] public float trailBreakDistance = 2f;

        public bool IsValid() => Positive(size) && Range(pulseAmount, 0f, 0.25f)
            && Nonnegative(pulseFrequency) && Nonnegative(trailSeconds) && Range(trailWidth, 0f, 1f)
            && Positive(trailBreakDistance);

        static bool Positive(float value) => float.IsFinite(value) && value > 0f;
        static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0f;
        static bool Range(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;

        // Starts at the authored size and gently breathes above it; never shrinks out of view.
        public float ScaleAt(float elapsed)
        {
            if (!IsValid())
            {
                return 1f;
            }
            elapsed = float.IsFinite(elapsed) ? Mathf.Max(0f, elapsed) : 0f;
            float wave = 0.5f - 0.5f * (float)System.Math.Cos((double)elapsed * pulseFrequency * System.Math.PI * 2d);
            float scale = Mathf.Max(0.1f, size) * (1f + wave * pulseAmount);
            return float.IsFinite(scale) ? scale : 1f;
        }
    }
}
