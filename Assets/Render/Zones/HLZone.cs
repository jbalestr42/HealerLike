using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>
    /// Cosmetic zone kind. The numeric values are part of the frozen GPU ABI
    /// (0 normal / 1 heal / 2 hostile ramp index on the shader side).
    /// </summary>
    public enum HLZoneKind : int { None = 0, Heal = 1, Hostile = 2 }

    /// <summary>
    /// One cosmetic zone record, laid out to match the 32-byte GPU element of _HL_Zones.
    /// Byte offsets 0/12/16/20/24/28, stride 32, no implicit tail: uploaded with SetData and
    /// read back in HLSL through the two 16-byte lanes of HLZoneStorage (see HLZoneData.hlsl).
    /// Position and radius are world units, strength 0..1, age is seconds since visual
    /// registration. Y is retained; grass evaluates influence in XZ. Radius is a radius.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct HLZone
    {
        [FieldOffset(0)]  public Vector3 position; // x 0, y 4, z 8
        [FieldOffset(12)] public float radius;
        [FieldOffset(16)] public int kind;
        [FieldOffset(20)] public float strength;
        [FieldOffset(24)] public float age;
        [FieldOffset(28)] public uint reserved;   // always zero

        /// <summary>Size in bytes of one GPU element; also the GraphicsBuffer stride.</summary>
        public const int Stride = 32;
    }

    /// <summary>
    /// Validation and canonicalization for the zone ABI. Pure, allocation-free and
    /// deterministic: the single zone owner (the zone registry) calls Pack once a frame
    /// before uploading. This type never allocates, never touches a GraphicsBuffer and
    /// knows nothing about expiry - duration and fade-out are producer responsibilities
    /// because the ABI has no duration field.
    /// </summary>
    public static class HLZonePacker
    {
        /// <summary>Hard capacity of the GPU buffer; the valid prefix is 0..64.</summary>
        public const int MaxZones = 64;

        /// <summary>
        /// Build a canonical zone record. Rejects non-finite fields, radius &lt;= 0 and
        /// unknown or None kinds; clamps strength into [0,1], clamps age to &gt;= 0 and
        /// clears reserved. On rejection <paramref name="zone"/> is default.
        /// </summary>
        public static bool TryCreate(Vector3 position, float radius, HLZoneKind kind,
            float strength, float age, out HLZone zone)
        {
            zone = default;

            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) return false;
            if (!IsFinite(radius) || !IsFinite(strength) || !IsFinite(age)) return false;
            if (radius <= 0f) return false;
            if (kind != HLZoneKind.Heal && kind != HLZoneKind.Hostile) return false;

            zone.position = position;
            zone.radius = radius;
            zone.kind = (int)kind;
            zone.strength = Mathf.Clamp01(strength);
            zone.age = age < 0f ? 0f : age;
            zone.reserved = 0u;
            return true;
        }

        /// <summary>
        /// Copy <paramref name="source"/> into <paramref name="destination"/> in the source's
        /// stable registration order, canonicalizing each item through <see cref="TryCreate"/>.
        /// Writes at most min(destination.Length, 64) records and zeroes the remaining
        /// destination slots, so the tail never carries stale data.
        /// Items that fail validation raise <paramref name="rejectedCount"/>; valid active items
        /// that did not fit raise <paramref name="overflowCount"/> (the first ones win, there is
        /// no reordering by camera distance). Valid items whose clamped strength is zero are
        /// inactive: they are silently omitted and counted as neither rejected nor overflow.
        /// </summary>
        /// <returns>The number of records written, i.e. the valid prefix to publish as _HL_ZoneCount.</returns>
        public static int Pack(ReadOnlySpan<HLZone> source, Span<HLZone> destination,
            out int rejectedCount, out int overflowCount)
        {
            rejectedCount = 0;
            overflowCount = 0;

            int capacity = Mathf.Min(destination.Length, MaxZones);
            int written = 0;

            for (int i = 0; i < source.Length; i++)
            {
                HLZone raw = source[i];
                if (!TryCreate(raw.position, raw.radius, (HLZoneKind)raw.kind, raw.strength, raw.age,
                        out HLZone canonical))
                {
                    rejectedCount++;
                    continue;
                }

                if (canonical.strength <= 0f) continue; // inactive, not an error

                if (written >= capacity)
                {
                    overflowCount++;
                    continue;
                }

                destination[written] = canonical;
                written++;
            }

            for (int i = written; i < destination.Length; i++) destination[i] = default;

            return written;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
