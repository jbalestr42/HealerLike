using System;
using UnityEngine;

namespace HealerLike.Render.Look
{
    // Authoring colours are sRGB, Validated returns a copy
    [Serializable]
    public struct LookSettings
    {
        static readonly float minimumFogDepth = 0.001f;

        public Color shadowTint;

        public Color outlineColor;

        public Color fogColor;

        public float shadowStrength;

        // Moves every surface's light and shade split: stones, ground, grass, heads, tips, effects and cast shadows.
        // Plant bodies add Look_Body.mat's offset to it.
        public float toonThreshold;

        // Half width of the soft terminator around the toon threshold
        public float toonSoftness;

        public float outlineWidthPixels;

        public float fogStart;

        public float fogEnd;

        public float inkStrength;

        public float inkScale;

        public float inkWidth;

        public float inkStart;

        public float inkRange;

        public float densityMul;

        public float inkWarp;

        public float inkWarpFreq;

        public float dashAmount;

        public float dashScale;

        public float inkDistStart;

        public float inkFarSpacing;

        // Contrast punch after the ink, before the fog
        public float contrast;

        public int fogBands;

        // The one source of the look's numbers: LookCore.hlsl repeats each as an HL_DEF_ define for a material
        // drawn without a LookController, and LookSettingsTests keeps the two equal
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
            value.shadowStrength = Mathf.Clamp(RenderMath.FiniteOr(shadowStrength, defaults.shadowStrength), 0.01f, 1f);
            value.toonThreshold =
                Mathf.Clamp(RenderMath.FiniteOr(toonThreshold, defaults.toonThreshold), 0.001f, 0.999f);
            value.toonSoftness = Mathf.Clamp(RenderMath.FiniteOr(toonSoftness, defaults.toonSoftness), 0f, 0.5f);
            value.outlineWidthPixels =
                Mathf.Max(0f, RenderMath.FiniteOr(outlineWidthPixels, defaults.outlineWidthPixels));
            value.fogStart = Mathf.Max(0f, RenderMath.FiniteOr(fogStart, defaults.fogStart));
            value.fogEnd = Mathf.Max(0f, RenderMath.FiniteOr(fogEnd, defaults.fogEnd));
            value.inkStrength = Mathf.Clamp(RenderMath.FiniteOr(inkStrength, defaults.inkStrength), 0f, 1f);
            value.inkScale = Mathf.Max(0.0001f, RenderMath.FiniteOr(inkScale, defaults.inkScale));
            value.inkWidth = Mathf.Max(0f, RenderMath.FiniteOr(inkWidth, defaults.inkWidth));
            value.inkStart = Mathf.Clamp(RenderMath.FiniteOr(inkStart, defaults.inkStart), 0f, 1f);
            value.inkRange = Mathf.Max(0.001f, RenderMath.FiniteOr(inkRange, defaults.inkRange));
            value.densityMul = Mathf.Clamp(RenderMath.FiniteOr(densityMul, defaults.densityMul), 0.01f, 1f);
            value.inkWarp = Mathf.Max(0f, RenderMath.FiniteOr(inkWarp, defaults.inkWarp));
            value.inkWarpFreq = Mathf.Max(0f, RenderMath.FiniteOr(inkWarpFreq, defaults.inkWarpFreq));
            value.dashAmount = Mathf.Clamp(RenderMath.FiniteOr(dashAmount, defaults.dashAmount), 0f, 0.92f);
            value.dashScale = Mathf.Max(0.001f, RenderMath.FiniteOr(dashScale, defaults.dashScale));
            value.inkDistStart = Mathf.Max(0.001f, RenderMath.FiniteOr(inkDistStart, defaults.inkDistStart));
            value.inkFarSpacing = Mathf.Max(0f, RenderMath.FiniteOr(inkFarSpacing, defaults.inkFarSpacing));
            value.contrast = Mathf.Clamp(RenderMath.FiniteOr(contrast, defaults.contrast), 1f, 1.6f);
            value.fogBands = Mathf.Max(1, fogBands);

            if (value.fogEnd - value.fogStart < minimumFogDepth)
            {
                value.fogEnd = value.fogStart + minimumFogDepth;
            }

            return value;
        }

        static Color ValidateColor(Color value, Color fallback, bool nonblack)
        {
            float red = Mathf.Clamp01(RenderMath.FiniteOr(value.r, fallback.r));
            float green = Mathf.Clamp01(RenderMath.FiniteOr(value.g, fallback.g));
            float blue = Mathf.Clamp01(RenderMath.FiniteOr(value.b, fallback.b));
            value = new Color(red, green, blue, 1f);
            if (nonblack && value.r == 0f && value.g == 0f && value.b == 0f)
            {
                return fallback;
            }

            return value;
        }
    }
}
