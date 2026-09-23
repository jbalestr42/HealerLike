using System;
using UnityEngine;

namespace HealerLike.Render.Look
{

    [Serializable]
    public struct HLLookSettings
    {
        public Color ShadowTint;
        public Color OutlineColor;
        public Color FogColor;
        public float ShadowStrength;
        public float ToonThreshold;
        public float OutlineWidthPixels;
        public float FogStart;
        public float FogEnd;
        public float InkStrength;
                public float InkSpacingPixels;
        public float InkScale;
        public float InkWidth;
        public float InkStart;
        public float InkRange;
        public float DensityMul;
        public float InkWarp;
        public float InkWarpFreq;
        public float DashAmount;
        public float DashScale;
        public float InkDistStart;
        public float InkFarSpacing;
        public int FogBands;

        public static HLLookSettings Default => new HLLookSettings
        {
            ShadowTint = new Color(43/255f, 75/255f, 143/255f, 1f),
            OutlineColor = new Color(24/255f, 38/255f, 63/255f, 1f),
            FogColor = new Color(191/255f, 210/255f, 224/255f, 1f),
            ShadowStrength = 0.65f,
            ToonThreshold = 0.5f,
            OutlineWidthPixels = 1f,
            FogStart = 20f,
            FogEnd = 60f,
            InkStrength = 1f,
            InkSpacingPixels = 3.5f,
            InkScale = 0.05f,
            InkWidth = 0.001f,
            InkStart = 0f,
            InkRange = 1f,
            DensityMul = 0.55f,
            InkWarp = 0.006f,
            InkWarpFreq = 2.44f,
            DashAmount = 0.1f,
            DashScale = 0.01f,
            InkDistStart = 10f,
            InkFarSpacing = 0.06f,
            FogBands = 6
        };

        public HLLookSettings Validated()
        {
            var value = this;
            var defaults = Default;
            value.ShadowTint = ValidateColor(ShadowTint, defaults.ShadowTint, true);
            value.OutlineColor = ValidateColor(OutlineColor, defaults.OutlineColor, true);
            value.FogColor = ValidateColor(FogColor, defaults.FogColor, false);
            value.ShadowStrength = Mathf.Clamp(Finite(ShadowStrength, defaults.ShadowStrength), 0.01f, 1f);
            value.ToonThreshold = Mathf.Clamp(Finite(ToonThreshold, defaults.ToonThreshold), 0.001f, 0.999f);
            value.OutlineWidthPixels = Mathf.Max(0f, Finite(OutlineWidthPixels, defaults.OutlineWidthPixels));
            value.FogStart = Mathf.Max(0f, Finite(FogStart, defaults.FogStart));
            value.FogEnd = Mathf.Max(0f, Finite(FogEnd, defaults.FogEnd));
            value.InkStrength = Mathf.Clamp(Finite(InkStrength, defaults.InkStrength), 0f, 1f);
            value.InkSpacingPixels = Mathf.Clamp(Finite(InkSpacingPixels, defaults.InkSpacingPixels), 0f, 16f);
            value.InkScale = Mathf.Max(0.0001f, Finite(InkScale, defaults.InkScale));
            value.InkWidth = Mathf.Max(0f, Finite(InkWidth, defaults.InkWidth));
            value.InkStart = Mathf.Clamp(Finite(InkStart, defaults.InkStart), 0f, 1f);
            value.InkRange = Mathf.Max(0.001f, Finite(InkRange, defaults.InkRange));
            value.DensityMul = Mathf.Clamp(Finite(DensityMul, defaults.DensityMul), 0.01f, 1f);
            value.InkWarp = Mathf.Max(0f, Finite(InkWarp, defaults.InkWarp));
            value.InkWarpFreq = Mathf.Max(0f, Finite(InkWarpFreq, defaults.InkWarpFreq));
            value.DashAmount = Mathf.Clamp(Finite(DashAmount, defaults.DashAmount), 0f, 0.92f);
            value.DashScale = Mathf.Max(0.001f, Finite(DashScale, defaults.DashScale));
            value.InkDistStart = Mathf.Max(0.001f, Finite(InkDistStart, defaults.InkDistStart));
            value.InkFarSpacing = Mathf.Max(0f, Finite(InkFarSpacing, defaults.InkFarSpacing));
            value.FogBands = Mathf.Max(1, FogBands);
            if (value.FogStart == float.MaxValue) value.FogStart = defaults.FogStart;
            if ((double)value.FogEnd - value.FogStart < .001)
            {
                value.FogEnd = (float)((double)value.FogStart + .001);
                if ((double)value.FogEnd - value.FogStart < .001)
                    value.FogEnd = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value.FogEnd) + 1);
            }
            return value;
        }

        static float Finite(float value, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        static Color ValidateColor(Color value, Color fallback, bool nonblack)
        {
            value = new Color(Mathf.Clamp01(Finite(value.r, fallback.r)),
                Mathf.Clamp01(Finite(value.g, fallback.g)), Mathf.Clamp01(Finite(value.b, fallback.b)), 1f);
            return nonblack && value.r == 0f && value.g == 0f && value.b == 0f ? fallback : value;
        }
    }
}
