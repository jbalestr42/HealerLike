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

            if (!RenderMath.IsFinite(position))
            {
                return false;
            }

            if (!float.IsFinite(radius) || !float.IsFinite(strength) || !float.IsFinite(age))
            {
                return false;
            }

            if (radius <= 0f)
            {
                return false;
            }

            if (kind < ZoneKind.Heal || kind > ZoneKind.Bruise)
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

                destination[written] = canonical;
                written++;
            }

            for (int i = written; i < destination.Length; i++)
            {
                destination[i] = default;
            }

            return written;
        }
    }
}
