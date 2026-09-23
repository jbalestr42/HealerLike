using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Look
{
    // Authoring colours are sRGB, Validated returns a copy
    [Serializable]
    public struct LookSettings
    {
        [FormerlySerializedAs("ShadowTint")]
        public Color shadowTint;

        [FormerlySerializedAs("OutlineColor")]
        public Color outlineColor;

        [FormerlySerializedAs("FogColor")]
        public Color fogColor;

        [FormerlySerializedAs("ShadowStrength")]
        public float shadowStrength;

        [FormerlySerializedAs("ToonThreshold")]
        public float toonThreshold;

        // Half width of the soft terminator around the toon threshold
        public float toonSoftness;

        [FormerlySerializedAs("OutlineWidthPixels")]
        public float outlineWidthPixels;

        [FormerlySerializedAs("FogStart")]
        public float fogStart;

        [FormerlySerializedAs("FogEnd")]
        public float fogEnd;

        [FormerlySerializedAs("InkStrength")]
        public float inkStrength;

        [FormerlySerializedAs("InkScale")]
        public float inkScale;

        [FormerlySerializedAs("InkWidth")]
        public float inkWidth;

        [FormerlySerializedAs("InkStart")]
        public float inkStart;

        [FormerlySerializedAs("InkRange")]
        public float inkRange;

        [FormerlySerializedAs("DensityMul")]
        public float densityMul;

        [FormerlySerializedAs("InkWarp")]
        public float inkWarp;

        [FormerlySerializedAs("InkWarpFreq")]
        public float inkWarpFreq;

        [FormerlySerializedAs("DashAmount")]
        public float dashAmount;

        [FormerlySerializedAs("DashScale")]
        public float dashScale;

        [FormerlySerializedAs("InkDistStart")]
        public float inkDistStart;

        [FormerlySerializedAs("InkFarSpacing")]
        public float inkFarSpacing;

        // Contrast punch after the ink, before the fog
        public float contrast;

        [FormerlySerializedAs("FogBands")]
        public int fogBands;

        public static LookSettings Default
        {
            get
            {
                return new LookSettings
                {
                    shadowTint = new Color(30 / 255f, 87 / 255f, 125 / 255f, 1f),
                    outlineColor = new Color(24 / 255f, 38 / 255f, 63 / 255f, 1f),
                    fogColor = new Color(154 / 255f, 188 / 255f, 211 / 255f, 1f),
                    shadowStrength = 0.7f,
                    toonThreshold = 0.45f,
                    toonSoftness = 0.08f,
                    outlineWidthPixels = 1f,
                    fogStart = 20f,
                    fogEnd = 60f,
                    inkStrength = 1f,
                    inkScale = 0.05f,
                    inkWidth = 0.0001f,
                    inkStart = 0.46f,
                    inkRange = 1f,
                    densityMul = 1f,
                    inkWarp = 0.06f,
                    inkWarpFreq = 2.44f,
                    dashAmount = 0.1f,
                    dashScale = 0.01f,
                    inkDistStart = 15f,
                    inkFarSpacing = 0.6f,
                    contrast = 1.15f,
                    fogBands = 6
                };
            }
        }

        public LookSettings Validated()
        {
            LookSettings value = this;
            LookSettings defaults = Default;
            value.shadowTint = ValidateColor(shadowTint, defaults.shadowTint, true);
            value.outlineColor = ValidateColor(outlineColor, defaults.outlineColor, true);
            value.fogColor = ValidateColor(fogColor, defaults.fogColor, false);
            value.shadowStrength = Mathf.Clamp(Finite(shadowStrength, defaults.shadowStrength), 0.01f, 1f);
            value.toonThreshold = Mathf.Clamp(Finite(toonThreshold, defaults.toonThreshold), 0.001f, 0.999f);
            value.toonSoftness = Mathf.Clamp(Finite(toonSoftness, defaults.toonSoftness), 0f, 0.5f);
            value.outlineWidthPixels = Mathf.Max(0f, Finite(outlineWidthPixels, defaults.outlineWidthPixels));
            value.fogStart = Mathf.Max(0f, Finite(fogStart, defaults.fogStart));
            value.fogEnd = Mathf.Max(0f, Finite(fogEnd, defaults.fogEnd));
            value.inkStrength = Mathf.Clamp(Finite(inkStrength, defaults.inkStrength), 0f, 1f);
            value.inkScale = Mathf.Max(0.0001f, Finite(inkScale, defaults.inkScale));
            value.inkWidth = Mathf.Max(0f, Finite(inkWidth, defaults.inkWidth));
            value.inkStart = Mathf.Clamp(Finite(inkStart, defaults.inkStart), 0f, 1f);
            value.inkRange = Mathf.Max(0.001f, Finite(inkRange, defaults.inkRange));
            value.densityMul = Mathf.Clamp(Finite(densityMul, defaults.densityMul), 0.01f, 1f);
            value.inkWarp = Mathf.Max(0f, Finite(inkWarp, defaults.inkWarp));
            value.inkWarpFreq = Mathf.Max(0f, Finite(inkWarpFreq, defaults.inkWarpFreq));
            value.dashAmount = Mathf.Clamp(Finite(dashAmount, defaults.dashAmount), 0f, 0.92f);
            value.dashScale = Mathf.Max(0.001f, Finite(dashScale, defaults.dashScale));
            value.inkDistStart = Mathf.Max(0.001f, Finite(inkDistStart, defaults.inkDistStart));
            value.inkFarSpacing = Mathf.Max(0f, Finite(inkFarSpacing, defaults.inkFarSpacing));
            value.contrast = Mathf.Clamp(Finite(contrast, defaults.contrast), 1f, 1.6f);
            value.fogBands = Mathf.Max(1, fogBands);

            // At the largest float there is no finite end greater than the start
            if (value.fogStart == float.MaxValue)
            {
                value.fogStart = defaults.fogStart;
            }

            if ((double)value.fogEnd - value.fogStart < 0.001)
            {
                value.fogEnd = (float)((double)value.fogStart + 0.001);
                // 0.001 can be smaller than the float spacing, then step to the next float up
                if ((double)value.fogEnd - value.fogStart < 0.001)
                {
                    value.fogEnd = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value.fogEnd) + 1);
                }
            }

            return value;
        }

        static float Finite(float value, float fallback)
        {
            return float.IsFinite(value) ? value : fallback;
        }

        static Color ValidateColor(Color value, Color fallback, bool nonblack)
        {
            float red = Mathf.Clamp01(Finite(value.r, fallback.r));
            float green = Mathf.Clamp01(Finite(value.g, fallback.g));
            float blue = Mathf.Clamp01(Finite(value.b, fallback.b));
            value = new Color(red, green, blue, 1f);
            return nonblack && value.r == 0f && value.g == 0f && value.b == 0f ? fallback : value;
        }
    }
}
