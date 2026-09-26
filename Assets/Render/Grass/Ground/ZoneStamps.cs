using System;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The published zones as ground stamps: heals and range previews lean the grass away from their centre,
    // obstacles flatten it outward, launches send a front along their heading. Hostile and bruise zones change
    // height only, which the tuft compute still reads from the zones.
    public static class ZoneStamps
    {
        // Radians of lean at a zone's full onset
        public static readonly float HealOutward = 0.5f;
        public static readonly float RangeOutward = 0.2f;
        public static readonly float TrampleOutward = 0.35f;
        public static readonly float LaunchLean = 0.6f;
        // A heal disc fades over this share of its radius, an obstacle over this one with a wobbling rim
        public static readonly float HealEdge = 0.25f;
        public static readonly float TrampleEdge = 0.15f;
        public static readonly float TrampleWobble = 0.24f;
        // A heal disc reaches its full radius after this many seconds, the bloom in HLLoadZone
        public static readonly float HealBloomSeconds = 0.3f;
        // The launch front crosses the radius in this many seconds in a band this share of it, at least MinBand
        public static readonly float LaunchSeconds = 0.4f;
        public static readonly float LaunchBand = 0.12f;
        public static readonly float LaunchMinBand = 0.15f;

        // HLGrassZoneOnset: the strength, eased in over the first 0.12 s
        public static float Onset(Zone zone)
        {
            return Mathf.Clamp01(zone.strength) * GroundStamp.SmoothStep(0f, 0.12f, zone.age);
        }

        // Writes one stamp per zone that moves grass, in order, and returns how many it wrote
        public static int Append(ReadOnlySpan<Zone> zones, GroundStamp[] into, int start)
        {
            if (into == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < zones.Length && start + count < into.Length; i++)
            {
                if (TryCreate(zones[i], out GroundStamp stamp))
                {
                    into[start + count] = stamp;
                    count++;
                }
            }

            return count;
        }

        public static bool TryCreate(Zone zone, out GroundStamp stamp)
        {
            stamp = new GroundStamp();
            float onset = Onset(zone);
            if (onset <= 0f || !RenderMath.IsPositive(zone.radius))
            {
                return false;
            }

            Vector2 centre = new Vector2(zone.position.x, zone.position.z);
            switch ((ZoneKind)zone.kind)
            {
                case ZoneKind.Heal:
                    float bloom = Mathf.Clamp01(zone.age / HealBloomSeconds);
                    stamp = GroundStamp.Disc(centre, zone.radius * bloom, HealOutward * onset, 0f, HealEdge, 0f);
                    return bloom > 0f;
                case ZoneKind.Range:
                    stamp = GroundStamp.Disc(centre, zone.radius, RangeOutward * onset, 0f, HealEdge, 0f);
                    return true;
                case ZoneKind.Trample:
                    stamp = GroundStamp.Disc(centre, zone.radius, TrampleOutward * onset, onset, TrampleEdge,
                                             TrampleWobble);
                    return true;
                case ZoneKind.Launch:
                    float front = zone.radius * Mathf.Clamp01(zone.age / LaunchSeconds);
                    float band = Mathf.Max(LaunchMinBand, zone.radius * LaunchBand);
                    stamp = GroundStamp.Front(centre, front, band, Heading(zone.reserved), LaunchLean * onset);
                    return true;
                default:
                    return false;
            }
        }

        // The launch heading ZonePacker.EncodeDirection stores, a full turn from +X toward +Z
        public static Vector2 Heading(uint turns)
        {
            float angle = turns * (Mathf.PI * 2f / 4294967296f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
