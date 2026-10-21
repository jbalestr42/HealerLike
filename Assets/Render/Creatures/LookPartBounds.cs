using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    public struct LookPartBounds
    {
        public float position;
        public float euler;
        public float size;
        public float glow;
        public int maxParts;

        public static LookPartBounds Creature
        {
            get { return new LookPartBounds { position = float.PositiveInfinity, euler = float.PositiveInfinity, size = float.PositiveInfinity, glow = float.PositiveInfinity, maxParts = 256 }; }
        }

        public static LookPartBounds Spell
        {
            get { return new LookPartBounds { position = 50f, euler = 3600f, size = 20f, glow = 10f, maxParts = 256 }; }
        }
    }

    public static class LookPartValidation
    {
        public static bool IsValid(LookPart part, LookPartBounds bounds)
        {
            bool enums = Enum.IsDefined(typeof(Primitive), part.primitive)
                && Enum.IsDefined(typeof(PartRole), part.role)
                && Enum.IsDefined(typeof(ColourRole), part.colour)
                && Enum.IsDefined(typeof(CountBand), part.minCount);
            return !string.IsNullOrEmpty(part.id) && enums
                && InRange(part.position, bounds.position) && InRange(part.euler, bounds.euler)
                && InRange(part.size, 0.001f, bounds.size) && InRange(part.glow, 0f, bounds.glow)
                && part.shape.IsValid();
        }

        static bool InRange(Vector3 value, float max) { return InRange(value, -max, max); }
        static bool InRange(Vector3 value, float min, float max)
        {
            return Finite(value.x) && Finite(value.y) && Finite(value.z)
                && value.x >= min && value.x <= max && value.y >= min && value.y <= max && value.z >= min && value.z <= max;
        }

        static bool InRange(float value, float min, float max) { return Finite(value) && value >= min && value <= max; }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
