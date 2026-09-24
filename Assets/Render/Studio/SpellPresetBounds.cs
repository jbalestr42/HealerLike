using System;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{
    // Bounds a preset's numbers, enums and parts to what the runtime effect draws; the authored values stay untouched
    public static class SpellPresetBounds
    {
        public static ElementEntry SanitizedEntry(ElementEntry source)
        {
            ElementEntry result = new ElementEntry();
            result.parts = SafeParts(source.parts);
            result.stackBeads = SafeParts(source.stackBeads);
            result.criticalRings = SafeParts(source.criticalRings);
            result.sideRim = SafeParts(source.sideRim);

            result.motion = Defined(source.motion, EffectMotionKind.Burst);
            result.socket = Defined(source.socket, EffectSocket.Body);
            result.count = Defined(source.count, EffectCount.Fixed);
            result.minCount = Mathf.Clamp(source.minCount, 0, EffectComposer.Shapes(result));
            result.cycleSeconds = Bounded(source.cycleSeconds, 0.6f, 0.01f, 120f);
            return result;
        }

        // A copy whose part arrays are the copy's own
        public static ElementEntry CloneEntry(ElementEntry source)
        {
            if (source == null)
            {
                return null;
            }

            ElementEntry copy = new ElementEntry();
            copy.parts = CopyParts(source.parts);
            copy.stackBeads = CopyParts(source.stackBeads);
            copy.criticalRings = CopyParts(source.criticalRings);
            copy.sideRim = CopyParts(source.sideRim);

            copy.motion = source.motion;
            copy.socket = source.socket;
            copy.count = source.count;
            copy.minCount = source.minCount;
            copy.cycleSeconds = source.cycleSeconds;
            return copy;
        }

        public static LookPart[] CopyParts(LookPart[] source)
        {
            if (source == null)
            {
                return Array.Empty<LookPart>();
            }
            return (LookPart[])source.Clone();
        }

        // At most MaxParts parts, each with a name, known enums and finite, bounded numbers
        public static LookPart[] SafeParts(LookPart[] source)
        {
            if (source == null)
            {
                return Array.Empty<LookPart>();
            }

            LookPart[] result = new LookPart[Mathf.Min(source.Length, SpellStudioPreset.MaxParts)];
            for (int i = 0; i < result.Length; i++)
            {
                LookPart part = source[i];
                if (string.IsNullOrEmpty(part.id))
                {
                    part.id = "Part " + (i + 1);
                }

                part.primitive = Defined(part.primitive, default(Primitive));
                part.role = Defined(part.role, PartRole.Body);
                part.colour = Defined(part.colour, ColourRole.Accent);
                part.minCount = Defined(part.minCount, default(CountBand));

                part.position = SafeVector(part.position, 0f, -50f, 50f);
                part.euler = SafeVector(part.euler, 0f, -3600f, 3600f);
                part.size = SafeVector(part.size, 0.1f, 0.001f, 20f);
                part.glow = Bounded(part.glow, 0f, 0f, 10f);
                result[i] = part;
            }
            return result;
        }

        public static EnumType Defined<EnumType>(EnumType value, EnumType fallback) where EnumType : struct
        {
            if (Enum.IsDefined(typeof(EnumType), value))
            {
                return value;
            }
            return fallback;
        }

        // Clamped into min..max, the fallback when the value is not finite
        public static float Bounded(float value, float fallback, float min, float max)
        {
            if (!float.IsFinite(value))
            {
                return fallback;
            }
            return Mathf.Clamp(value, min, max);
        }

        public static Vector3 SafeVector(Vector3 value, float fallback, float min, float max)
        {
            float x = Bounded(value.x, fallback, min, max);
            float y = Bounded(value.y, fallback, min, max);
            float z = Bounded(value.z, fallback, min, max);
            return new Vector3(x, y, z);
        }

        public static bool InRange(float value, float min, float max)
        {
            return float.IsFinite(value) && value >= min && value <= max;
        }

        public static bool InRange(Vector3 value, float min, float max)
        {
            return InRange(value.x, min, max) && InRange(value.y, min, max) && InRange(value.z, min, max);
        }

        // HDR channels up to 16, alpha up to 1
        public static bool IsValidColour(Color value)
        {
            bool isRgb = InRange(value.r, 0f, 16f) && InRange(value.g, 0f, 16f) && InRange(value.b, 0f, 16f);
            return isRgb && InRange(value.a, 0f, 1f);
        }

        public static Color SafeColour(Color value)
        {
            float r = Bounded(value.r, 1f, 0f, 16f);
            float g = Bounded(value.g, 1f, 0f, 16f);
            float b = Bounded(value.b, 1f, 0f, 16f);
            float a = Bounded(value.a, 1f, 0f, 1f);
            return new Color(r, g, b, a);
        }
    }
}
