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

        // Starts at the authored size and gently breathes above it; never shrinks out of view.
        public float ScaleAt(float elapsed)
        {
            float wave = 0.5f - 0.5f * Mathf.Cos(Mathf.Max(0f, elapsed) * Mathf.Max(0f, pulseFrequency)
                * Mathf.PI * 2f);
            return Mathf.Max(0.1f, size) * (1f + wave * Mathf.Clamp(pulseAmount, 0f, 0.25f));
        }
    }
}
