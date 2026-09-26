using System;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The published zones as ground stamps. Heals and range previews hold the grass leaning away from their
    // centre and a heal also spins it and makes it grow and glow; obstacles flatten it outward; launches throw it
    // along their heading and shocks throw it outward in a ring; ash and wilt zones burn or kill it. Hostile and
    // bruise zones change height only, which the tuft compute still reads from the zones.
    public static class ZoneStamps
    {
        // Radians of held lean at a zone's full onset
        public static readonly float HealOutward = 0.5f;
        public static readonly float RangeOutward = 0.2f;
        public static readonly float TrampleOutward = 0.35f;
        // Accelerations at full strength, in radians per second squared
        public static readonly float HealSwirl = 30f;
        public static readonly float LaunchKick = 90f;
        public static readonly float ShockKick = 120f;
        // A heal disc fades over this share of its radius, an obstacle over this one with a wobbling rim
        public static readonly float HealEdge = 0.25f;
        public static readonly float SwirlEdge = 0.4f;
        public static readonly float TrampleEdge = 0.15f;
        public static readonly float TrampleWobble = 0.24f;
        // A heal disc reaches its full radius after this many seconds, the bloom in HLLoadZone
        public static readonly float HealBloomSeconds = 0.3f;
        // The launch front crosses the radius in this many seconds, inside the registry's pulse, in a band this share
        // of it, at least MinBand
        public static readonly float LaunchSeconds = 0.3f;
        public static readonly float LaunchBand = 0.12f;
        public static readonly float LaunchMinBand = 0.15f;
        // A shock ring reaches its radius after this many seconds, in a band this share of it, at least MinBand
        public static readonly float ShockSeconds = 0.35f;
        public static readonly float ShockBand = 0.15f;
        public static readonly float ShockMinBand = 0.2f;
        // A quarter turn counterclockwise: the heal spins the grass around its centre
        public static readonly float SwirlTurn = Mathf.PI * 0.5f;
        // Aura edges as shares of their radius, and how far their rims wobble inward
        public static readonly float AshEdge = 0.35f;
        public static readonly float AshWobble = 0.22f;
        public static readonly float WiltEdge = 0.5f;
        public static readonly float WiltWobble = 0.15f;
        public static readonly float BloomEdge = 0.4f;

        // HLGrassZoneOnset: the strength, eased in over the first 0.12 s
        public static float Onset(Zone zone)
        {
            return Mathf.Clamp01(zone.strength) * GroundStamp.SmoothStep(0f, 0.12f, zone.age);
        }

        // Writes every stamp of every zone that moves grass, in order, as many as fit, and returns how many
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

                if (start + count < into.Length && TryCreateKick(zones[i], out GroundStamp kick))
                {
                    into[start + count] = kick;
                    count++;
                }

                if (start + count < into.Length && TryCreateAura(zones[i], out GroundStamp aura))
                {
                    into[start + count] = aura;
                    count++;
                }
            }

            return count;
        }

        // The zone's main stamp: what it holds, or for a launch or a shock what it throws
        public static bool TryCreate(Zone zone, out GroundStamp stamp)
        {
            stamp = new GroundStamp();
            if (!RenderMath.IsPositive(zone.radius) || !RenderMath.IsPositive(zone.strength))
            {
                return false;
            }

            float onset = Onset(zone);
            float strength = Mathf.Clamp01(zone.strength);
            Vector2 centre = new Vector2(zone.position.x, zone.position.z);
            switch ((ZoneKind)zone.kind)
            {
                case ZoneKind.Heal:
                    float bloom = Mathf.Clamp01(zone.age / HealBloomSeconds);
                    stamp = GroundStamp.Disc(centre, zone.radius * bloom, HealOutward * onset, 0f, HealEdge, 0f);
                    return bloom > 0f && onset > 0f;
                case ZoneKind.Range:
                    stamp = GroundStamp.Disc(centre, zone.radius, RangeOutward * onset, 0f, HealEdge, 0f);
                    return onset > 0f;
                case ZoneKind.Trample:
                    stamp = GroundStamp.Disc(centre, zone.radius, TrampleOutward * onset, onset, TrampleEdge,
                                             TrampleWobble);
                    return onset > 0f;
                case ZoneKind.Launch:
                    float front = zone.radius * Mathf.Clamp01(zone.age / LaunchSeconds);
                    float band = Mathf.Max(LaunchMinBand, zone.radius * LaunchBand);
                    stamp = GroundStamp.Front(centre, front, band, Heading(zone.reserved), 1f,
                                              LaunchKick * LaunchStrength(zone));
                    return true;
                case ZoneKind.Shock:
                    float ring = zone.radius * Mathf.Clamp01(zone.age / ShockSeconds);
                    float width = Mathf.Max(ShockMinBand, zone.radius * ShockBand);
                    stamp = GroundStamp.Shock(centre, ring, width, 1f, ShockKick * strength);
                    return true;
                default:
                    return false;
            }
        }

        // A second stamp some zones throw on top of their main one: a heal spins the grass while it blooms
        public static bool TryCreateKick(Zone zone, out GroundStamp stamp)
        {
            stamp = new GroundStamp();
            if ((ZoneKind)zone.kind != ZoneKind.Heal || !RenderMath.IsPositive(zone.radius))
            {
                return false;
            }

            float onset = Onset(zone);
            float bloom = Mathf.Clamp01(zone.age / HealBloomSeconds);
            stamp = GroundStamp.Swirl(new Vector2(zone.position.x, zone.position.z), zone.radius * bloom, SwirlTurn,
                                      1f, HealSwirl * onset, SwirlEdge);
            return bloom > 0f && onset > 0f;
        }

        // A launch's front keeps the strength it started with while it crosses; the registry fades the pulse
        // over its whole life, which would spend the front before it arrives
        public static float LaunchStrength(Zone zone)
        {
            float left = 1f - zone.age / ZoneRegistry.LaunchSeconds;
            return Mathf.Clamp01(zone.strength / Mathf.Max(0.05f, left));
        }

        // What a zone asks of the ground state: ash for a rocky enemy, dead grass for a hurt ally, lush glowing
        // grass for a heal
        public static bool TryCreateAura(Zone zone, out GroundStamp stamp)
        {
            stamp = new GroundStamp();
            if (!RenderMath.IsPositive(zone.radius) || !RenderMath.IsPositive(zone.strength))
            {
                return false;
            }

            Vector2 centre = new Vector2(zone.position.x, zone.position.z);
            float strength = Mathf.Clamp01(zone.strength);
            switch ((ZoneKind)zone.kind)
            {
                case ZoneKind.Ash:
                    stamp = GroundStamp.Aura(centre, zone.radius, AshEdge, AshWobble, strength, 0f, 0f);
                    return true;
                case ZoneKind.Wilt:
                    stamp = GroundStamp.Aura(centre, zone.radius, WiltEdge, WiltWobble, 0f, -strength, 0f);
                    return true;
                case ZoneKind.Heal:
                    float bloom = Mathf.Clamp01(zone.age / HealBloomSeconds);
                    float onset = Onset(zone);
                    stamp = GroundStamp.Aura(centre, zone.radius * bloom, BloomEdge, 0f, 0f, onset, onset);
                    return bloom > 0f && onset > 0f;
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
