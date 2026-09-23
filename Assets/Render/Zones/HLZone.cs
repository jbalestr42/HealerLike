using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public enum HLZoneKind : int
    {
        None = 0,
        Heal = 1,
        Hostile = 2,
        Range = 3,
        Bruise = 4,
        Launch = 5,
        Trample = 6
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct HLZone
    {
        [FieldOffset(0)]  public Vector3 position;
        [FieldOffset(12)] public float radius;
        [FieldOffset(16)] public int kind;
        [FieldOffset(20)] public float strength;
        [FieldOffset(24)] public float age;
        [FieldOffset(28)] public uint reserved;

        public static readonly int Stride = 32;
    }

    public static class HLZonePacker
    {

        public static readonly int MaxZones = 64;

        public static bool TryCreate(Vector3 position, float radius, HLZoneKind kind,
            float strength, float age, out HLZone zone)
        {
            zone = default;

            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) return false;
            if (!IsFinite(radius) || !IsFinite(strength) || !IsFinite(age)) return false;
            if (radius <= 0f) return false;
            if (kind < HLZoneKind.Heal || kind > HLZoneKind.Trample) return false;

            zone.position = position;
            zone.radius = radius;
            zone.kind = (int)kind;
            zone.strength = Mathf.Clamp01(strength);
            zone.age = age < 0f ? 0f : age;
            zone.reserved = 0u;
            return true;
        }

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

                if (canonical.strength <= 0f) continue;

                if (written >= capacity)
                {
                    overflowCount++;
                    continue;
                }

                if (canonical.kind == (int)HLZoneKind.Launch) canonical.reserved = raw.reserved;
                destination[written] = canonical;
                written++;
            }

            for (int i = written; i < destination.Length; i++) destination[i] = default;

            return written;
        }

        public static uint EncodeDirection(Vector3 direction)
        {
            if (!IsFinite(direction.x) || !IsFinite(direction.z)) return 0;
            double turns = System.Math.Atan2(direction.z, direction.x) / (2 * System.Math.PI);
            if (turns < 0) turns += 1;
            return (uint)(turns * 4294967296.0);
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
