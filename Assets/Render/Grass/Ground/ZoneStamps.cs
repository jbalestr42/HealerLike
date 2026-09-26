using System;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The published zones as ground stamps. Heals and range previews hold the grass leaning away from their
    // centre and a heal also spins it and makes it grow and glow; obstacles flatten it outward; a shot in flight
    // parts it along its trail, a shock throws it outward in a ring and a coming enemy area shivers it. Ash and
    // wilt zones burn or kill it, boosted cells light it, poison blights it, a slow frosts it and lightning
    // scorches a jagged line into it. Hostile and bruise zones change height only, which the tuft compute still
    // reads from the zones.
    public static class ZoneStamps
    {
        // Radians of held lean at a zone's full onset
        public static readonly float HealOutward = 0.5f;
        public static readonly float RangeOutward = 0.2f;
        public static readonly float TrampleOutward = 0.35f;
        // Accelerations at full strength, in radians per second squared
        public static readonly float HealSwirl = 30f;
        public static readonly float LaunchKick = 110f;
        public static readonly float ShockKick = 170f;
        // A heal disc fades over this share of its radius, an obstacle over this one with a wobbling rim
        public static readonly float HealEdge = 0.25f;
        public static readonly float SwirlEdge = 0.4f;
        public static readonly float TrampleEdge = 0.15f;
        public static readonly float TrampleWobble = 0.24f;
        // A heal disc reaches its full radius after this many seconds, the bloom in HLLoadZone
        public static readonly float HealBloomSeconds = 0.3f;
        // A shot parts the grass this wide either side of its trail, in world units
        public static readonly float LaunchWidth = 0.45f;
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
        public static readonly float BoostEdge = 0.6f;
        public static readonly float BoostWobble = 0.12f;
        // A boosted cell's grass: this lush and this lit
        public static readonly float BoostVitality = 0.6f;
        public static readonly float BoostGlow = 0.3f;
        public static readonly float BlightEdge = 0.5f;
        public static readonly float BlightWobble = 0.2f;
        public static readonly float FrostEdge = 0.4f;
        public static readonly float FrostWobble = 0.25f;
        // A bolt's scorch: this half wide at its start, zigzagging this far either side, this many turns per unit
        public static readonly float ScorchWidth = 0.12f;
        public static readonly float ScorchSwing = 0.15f;
        public static readonly float ScorchTurns = 1.8f;
        // A warning shivers the grass outward and back this many times a second, this hard at its strongest
        public static readonly float TrembleRate = 5f;
        public static readonly float TrembleKick = 320f;
        public static readonly float TrembleEdge = 0.25f;

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
                    // The zone sits under the projectile, its radius the trail behind it along its heading
                    Vector2 behind = centre - Heading(zone.reserved) * zone.radius;
                    stamp = GroundStamp.Trail(behind, centre, LaunchWidth, LaunchKick * strength);
                    return true;
                case ZoneKind.Tremble:
                    float shiver = Mathf.Sin(zone.age * TrembleRate * 2f * Mathf.PI);
                    stamp = GroundStamp.Swirl(centre, zone.radius, 0f, 1f, TrembleKick * strength * shiver, TrembleEdge);
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
                case ZoneKind.Boost:
                    stamp = GroundStamp.Aura(centre, zone.radius, BoostEdge, BoostWobble, 0f,
                                             BoostVitality * strength, BoostGlow * strength);
                    return true;
                case ZoneKind.Blight:
                    stamp = GroundStamp.Aura(centre, zone.radius, BlightEdge, BlightWobble, 0f, 0f, 0f, strength);
                    return true;
                case ZoneKind.Frost:
                    stamp = GroundStamp.Aura(centre, zone.radius, FrostEdge, FrostWobble, 0f, 0f, -strength);
                    return true;
                case ZoneKind.Scorch:
                    Vector2 end = centre + Heading(zone.reserved) * zone.radius;
                    stamp = GroundStamp.Streak(centre, end, ScorchWidth, ScorchSwing, ScorchTurns, strength, 0f, 0f,
                                               0f);
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
