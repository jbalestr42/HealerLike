using System;
using UnityEngine;

namespace HealerLike.Render.Look
{
    // The shared C# look contract: each published setting belongs to the same raw snapshot and restore.
    public static class LookShaderProperties
    {
        readonly struct FloatProperty
        {
            public readonly int id;
            public readonly Func<LookSettings, float> read;

            public FloatProperty(string name, Func<LookSettings, float> read)
            {
                id = Shader.PropertyToID(name);
                this.read = read;
            }
        }

        readonly struct ColorProperty
        {
            public readonly int id;
            public readonly Func<LookSettings, Color> read;

            public ColorProperty(string name, Func<LookSettings, Color> read)
            {
                id = Shader.PropertyToID(name);
                this.read = read;
            }
        }

        static readonly int appliedId = Shader.PropertyToID("_HLLookApplied");
        static readonly FloatProperty[] floats =
        {
            new FloatProperty("_HLShadowStrength", value => value.shadowStrength),
            new FloatProperty("_HLToonThreshold", value => value.toonThreshold),
            new FloatProperty("_HLToonSoftness", value => value.toonSoftness),
            new FloatProperty("_HLOutlineWidthPixels", value => value.outlineWidthPixels),
            new FloatProperty("_HLFogStart", value => value.fogStart),
            new FloatProperty("_HLFogEnd", value => value.fogEnd),
            new FloatProperty("_HLFogBands", value => value.fogBands),
            new FloatProperty("_HLInkStrength", value => value.inkStrength),
            new FloatProperty("_HLInkScale", value => value.inkScale),
            new FloatProperty("_HLInkWidth", value => value.inkWidth),
            new FloatProperty("_HLInkStart", value => value.inkStart),
            new FloatProperty("_HLInkRange", value => value.inkRange),
            new FloatProperty("_HLDensityMul", value => value.densityMul),
            new FloatProperty("_HLInkWarp", value => value.inkWarp),
            new FloatProperty("_HLInkWarpFreq", value => value.inkWarpFreq),
            new FloatProperty("_HLDashAmount", value => value.dashAmount),
            new FloatProperty("_HLDashScale", value => value.dashScale),
            new FloatProperty("_HLInkDistStart", value => value.inkDistStart),
            new FloatProperty("_HLInkFarSpacing", value => value.inkFarSpacing),
            new FloatProperty("_HLContrast", value => value.contrast)
        };
        static readonly ColorProperty[] colors =
        {
            new ColorProperty("_HLShadowTint", value => value.shadowTint),
            new ColorProperty("_HLOutlineColor", value => value.outlineColor),
            new ColorProperty("_HLFogColor", value => value.fogColor)
        };

        // Settings are already validated by the owner. Studio values use the same conversion as the stage.
        public static void Publish(LookSettings settings, ColorSpace space)
        {
            foreach (FloatProperty property in floats)
            {
                Shader.SetGlobalFloat(property.id, property.read(settings));
            }
            foreach (ColorProperty property in colors)
            {
                Shader.SetGlobalVector(property.id, ToWorkingColor(property.read(settings), space));
            }
            Shader.SetGlobalFloat(appliedId, 1f);
        }

        public static void ClearApplied()
        {
            Shader.SetGlobalFloat(appliedId, 0f);
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace space)
        {
            Color color = space == ColorSpace.Linear ? srgb.linear : srgb;
            return new Vector4(color.r, color.g, color.b, 1f);
        }

        public static Snapshot Capture()
        {
            return new Snapshot();
        }

        // Preserve raw shader values, including alpha and the applied flag, without validation or conversion.
        public class Snapshot
        {
            readonly float[] _floats = new float[floats.Length];
            readonly Vector4[] _vectors = new Vector4[colors.Length];
            readonly float _applied;

            internal Snapshot()
            {
                for (int i = 0; i < floats.Length; i++)
                {
                    _floats[i] = Shader.GetGlobalFloat(floats[i].id);
                }
                for (int i = 0; i < colors.Length; i++)
                {
                    _vectors[i] = Shader.GetGlobalVector(colors[i].id);
                }
                _applied = Shader.GetGlobalFloat(appliedId);
            }

            public void Restore()
            {
                for (int i = 0; i < floats.Length; i++)
                {
                    Shader.SetGlobalFloat(floats[i].id, _floats[i]);
                }
                for (int i = 0; i < colors.Length; i++)
                {
                    Shader.SetGlobalVector(colors[i].id, _vectors[i]);
                }
                Shader.SetGlobalFloat(appliedId, _applied);
            }
        }
    }
}
