using System;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public static class ZonePacker
    {
        public static readonly int MaxZones = 64;

        public static bool TryCreate(Vector3 position, float radius, ZoneKind kind, float strength, float age,
                                     out Zone zone)
        {
            zone = default;

            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                return false;
            }

            if (!IsFinite(radius) || !IsFinite(strength) || !IsFinite(age))
            {
                return false;
            }

            if (radius <= 0f)
            {
                return false;
            }

            if (kind < ZoneKind.Heal || kind > ZoneKind.Trample)
            {
                return false;
            }

            zone.position = position;
            zone.radius = radius;
            zone.kind = (int)kind;
            zone.strength = Mathf.Clamp01(strength);
            zone.age = age < 0f ? 0f : age;
            zone.reserved = 0u;
            return true;
        }

        // Not a sort: keeps room for the gameplay feedback first and gives the footprints what is left,
        // moving the kept zones to the front in their order and returning how many were kept
        public static int ReserveFeedback(Zone[] zones, int count)
        {
            int feedback = 0;
            for (int i = 0; i < count; i++)
            {
                if (zones[i].kind != (int)ZoneKind.Trample)
                {
                    feedback++;
                }
            }

            int footprints = Mathf.Max(0, MaxZones - feedback);
            int selected = 0;
            for (int i = 0; i < count; i++)
            {
                if (zones[i].kind == (int)ZoneKind.Trample && footprints-- <= 0)
                {
                    continue;
                }

                zones[selected] = zones[i];
                selected++;
            }
            return selected;
        }

        // Keeps the source order, the first ones win when the destination is full
        public static int Pack(ReadOnlySpan<Zone> source, Span<Zone> destination, out int rejectedCount,
                               out int overflowCount)
        {
            rejectedCount = 0;
            overflowCount = 0;

            int capacity = Mathf.Min(destination.Length, MaxZones);
            int written = 0;

            for (int i = 0; i < source.Length; i++)
            {
                Zone raw = source[i];
                if (!TryCreate(raw.position, raw.radius, (ZoneKind)raw.kind, raw.strength, raw.age,
                               out Zone canonical))
                {
                    rejectedCount++;
                    continue;
                }

                // A zero strength zone is inactive, not an error
                if (canonical.strength <= 0f)
                {
                    continue;
                }

                if (written >= capacity)
                {
                    overflowCount++;
                    continue;
                }

                if (canonical.kind == (int)ZoneKind.Launch)
                {
                    canonical.reserved = raw.reserved;
                }

                destination[written] = canonical;
                written++;
            }

            for (int i = written; i < destination.Length; i++)
            {
                destination[i] = default;
            }

            return written;
        }

        // Full uint turn: +X is 0, +Z is a quarter turn, the shader decodes it the same way
        public static uint EncodeDirection(Vector3 direction)
        {
            if (!IsFinite(direction.x) || !IsFinite(direction.z))
            {
                return 0;
            }

            double turns = System.Math.Atan2(direction.z, direction.x) / (2 * System.Math.PI);
            if (turns < 0)
            {
                turns += 1;
            }

            return (uint)(turns * 4294967296.0);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
