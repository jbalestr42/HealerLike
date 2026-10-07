using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Authored presentation, independent of gameplay clocks and element labels.
    [Serializable]
    public class EffectPresentation
    {
        public bool enabled;
        public float scale = 1f;
        public float entranceSeconds = .12f;
        public float releaseSeconds = .35f;
        public float motionSpan = .45f;
        public float idleVisibility = .35f;
        public bool billboard;
        public float cameraDepth;
        public bool closesOverHead;
        public bool scalesWithAmount;
        public bool isShield;
        public bool avoidHead = true;
        public ColourRole colourRole = ColourRole.Accent;
        public float linkWidth = .04f;
        public float linkBeadSeconds = .85f;

        public EffectPresentation Clone() => (EffectPresentation)MemberwiseClone();
        public bool IsValid() => Positive(scale) && Nonnegative(cameraDepth) && Nonnegative(entranceSeconds)
            && Positive(releaseSeconds) && Positive(motionSpan) && Positive(linkWidth)
            && Positive(linkBeadSeconds) && float.IsFinite(idleVisibility)
            && idleVisibility >= 0 && idleVisibility <= 1 && Enum.IsDefined(typeof(ColourRole), colourRole);
        static bool Positive(float x) => float.IsFinite(x) && x > 0;
        static bool Nonnegative(float x) => float.IsFinite(x) && x >= 0;
    }

    public static class EffectEnvelope
    {
        // Keep a clear peak, then resolve smoothly. Never changes consumer or projectile timing.
        public static float Visibility(EffectPresentation profile, float age, float lifetime, bool held)
        {
            if (profile == null || !profile.enabled)
            {
                return 1f;
            }

            float enter = profile.entranceSeconds <= 0 ? 1 : Mathf.SmoothStep(.28f, 1,
                Mathf.Clamp01(age / profile.entranceSeconds));
            if (held)
            {
                return enter;
            }

            float release = Mathf.Min(profile.releaseSeconds, lifetime * .45f);
            return enter * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(lifetime - release, lifetime, age)));
        }
    }
}
