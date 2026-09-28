using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A capture-only delta on the field's single authored source, GrassBlade.mat: its base colour in HSV and the
    // strength of its blade-to-shadow contrast (shade tint, shade turn and hatch ink). Applied in memory for one
    // frame and restored; nothing here is a tuning value and nothing is saved to the asset.
    [Serializable]
    public struct FieldTreatment
    {
        public string name;
        public float valueScale;
        public float saturationScale;
        // Scales the shade tint strength, the shade turn strength and the hatch ink together
        public float contrastScale;
        // Negative keeps the authored hue; else the hue, 0 to 1, the colour moves to
        public float hue;

        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int shadeTintId = Shader.PropertyToID("_HLShadeTint");
        static readonly int shadeTurnTintId = Shader.PropertyToID("_HLShadeTurnTint");
        static readonly int hatchId = Shader.PropertyToID("_HLHatchMultiplier");

        public static FieldTreatment Control()
        {
            return new FieldTreatment { name = "control", valueScale = 1f, saturationScale = 1f, contrastScale = 1f, hue = -1f };
        }

        // Treatments 1, 2 and 3 at one strength: 0 is the control, 1 is each at its full delta
        public static FieldTreatment Combination(float strength)
        {
            return new FieldTreatment
            {
                name = "combination", hue = -1f, valueScale = 1f - .35f * strength,
                saturationScale = 1f - .4f * strength, contrastScale = 1f - .5f * strength
            };
        }

        // The sheet's six treatments, in order, each a delta from the committed state
        public static FieldTreatment[] Sheet(float combinationStrength)
        {
            FieldTreatment value = Control(), saturation = Control(), hatch = Control(), hue = Control();
            value.name = "value-only";
            value.valueScale = .65f;
            saturation.name = "saturation-and-value";
            saturation.saturationScale = .6f;
            saturation.valueScale = .7f;
            hatch.name = "hatch-contrast";
            hatch.contrastScale = .5f;
            // Deep teal-green: the committed hue is about 142 degrees, the lime creatures sit below it
            hue.name = "hue-separation";
            hue.hue = 165f / 360f;
            hue.valueScale = .6f;
            return new[] { Control(), value, saturation, hatch, hue, Combination(combinationStrength) };
        }

        // The base colour as authored, in the material's stored sRGB
        public Color Treat(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            if (hue >= 0f) h = hue;
            Color treated = Color.HSVToRGB(h, Mathf.Clamp01(s * saturationScale), Mathf.Clamp01(v * valueScale));
            treated.a = color.a;
            return treated;
        }

        public Snapshot Apply(Material material)
        {
            Snapshot saved = Snapshot.Read(material);
            material.SetColor(baseColorId, Treat(saved.baseColor));
            material.SetColor(shadeTintId, Strength(saved.shadeTint, contrastScale));
            material.SetColor(shadeTurnTintId, Strength(saved.shadeTurnTint, contrastScale));
            material.SetFloat(hatchId, saved.hatch * contrastScale);
            return saved;
        }

        static Color Strength(Color tint, float scale)
        {
            tint.a *= scale;
            return tint;
        }

        // The authored values a treatment replaces, restored exactly after the frame
        public struct Snapshot
        {
            public Color baseColor, shadeTint, shadeTurnTint;
            public float hatch;

            public static Snapshot Read(Material material)
            {
                return new Snapshot
                {
                    baseColor = material.GetColor(baseColorId), shadeTint = material.GetColor(shadeTintId),
                    shadeTurnTint = material.GetColor(shadeTurnTintId), hatch = material.GetFloat(hatchId)
                };
            }

            public void Restore(Material material)
            {
                material.SetColor(baseColorId, baseColor);
                material.SetColor(shadeTintId, shadeTint);
                material.SetColor(shadeTurnTintId, shadeTurnTint);
                material.SetFloat(hatchId, hatch);
            }
        }
    }
}
