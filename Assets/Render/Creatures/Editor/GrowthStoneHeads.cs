using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    // Complete shape vocabulary from the approved Growth and stone reference, independent of entity names.
    public static class GrowthStoneHeads
    {
        public static LookVocabulary.HeadEntry Build(HeadKind kind)
        {
            return new LookVocabulary.HeadEntry
            {
                plant = Build(kind, false), stone = Build(kind, true), carriesCount = kind == HeadKind.Arch,
                plantStemScale = NeckScale(kind)
            };
        }

        static float NeckScale(HeadKind kind)
        {
            switch (kind)
            {
                case HeadKind.Spear: return 0.95f;
                case HeadKind.Arch: return 0.6f;
                case HeadKind.Fork:
                case HeadKind.GiftBoonDefence: return 0.25f;
                case HeadKind.Conductor: return 0.35f;
                case HeadKind.GiftHeal: return 0.7f;
                case HeadKind.GiftBane: return 0.55f;
                case HeadKind.SelfTick: return 0.6f;
                default: return 0.45f;
            }
        }

        static LookPart[] Build(HeadKind kind, bool stone)
        {
            List<LookPart> parts = new List<LookPart>();
            ShapeProfile round = stone ? ShapeProfile.Block(0.3f, 0.12f, 0.13f) : ShapeProfile.Bulb();
            ShapeProfile leaf = stone ? ShapeProfile.Shard(0.82f, 0.04f) : ShapeProfile.Leaf(0.35f, 0.82f);
            switch (kind)
            {
                case HeadKind.Bud:
                    parts.Add(Anchored("BudBody", round, new Vector3(0f, 0.01f, 0f),
                        new Vector3(1.17f, 1.12f, 1f), Quaternion.identity));
                    AttachedTip(parts, stone, "BudBody", new Vector3(0.14f, 0.22f, 0.14f));
                    break;
                case HeadKind.Spear:
                    parts.Add(Part("SpearJoint", round, new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 0.2f)));
                    ShapeProfile lance = stone ? leaf : ShapeProfile.Leaf(0.2f, 0.65f);
                    if (!stone) lance.taper = 0.78f;
                    parts.Add(Anchored("SpearBlade", lance, new Vector3(0f, 0.21f, 0f),
                        stone ? new Vector3(0.5f, 1.35f, 0.36f) : new Vector3(0.48f, 1.8f, 0.31f), Quaternion.identity));
                    AttachedTip(parts, stone, "SpearBlade", new Vector3(0.11f, 0.2f, 0.11f));
                    break;
                case HeadKind.Arch:
                    Arch(parts, stone);
                    break;
                case HeadKind.Conductor:
                    parts.Add(Part("ConductorCollar", ShapeProfile.Ring(0.23f, stone), new Vector3(0f, 0.28f, 0f),
                        new Vector3(1.55f, 0.3f, 1.18f)));
                    parts.Add(Part("ConductorMast", stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                        new Vector3(0f, 0.53f, 0f), new Vector3(0.24f, 1.02f, 0.24f)));
                    parts.Add(Part("ConductorLower", round, new Vector3(0f, 0.74f, 0f), new Vector3(0.62f, 0.58f, 0.54f)));
                    parts.Add(Part("ConductorUpper", round, new Vector3(-0.035f, 1.22f, 0f), new Vector3(0.39f, 0.44f, 0.36f)));
                    AttachedTip(parts, stone, "ConductorUpper", new Vector3(0.14f, 0.19f, 0.14f));
                    break;
                case HeadKind.Fork:
                    Fork(parts, stone);
                    break;
                case HeadKind.GiftHeal:
                    if (stone)
                    {
                        parts.Add(Anchored("HealPedestal", ShapeProfile.Block(0.16f), Vector3.zero,
                            new Vector3(0.92f, 0.38f, 0.72f), Quaternion.identity));
                        parts.Add(Anchored("HealCore", ShapeProfile.Block(0.22f), new Vector3(0f, 0.3f, 0f),
                            new Vector3(0.51f, 0.54f, 0.47f), Quaternion.identity));
                        for (int side = -1; side <= 1; side += 2)
                        {
                            parts.Add(Anchored("HealSeed", ShapeProfile.Block(0.22f), new Vector3(side * 0.32f, 0.3f, 0f),
                                new Vector3(0.35f, 0.4f, 0.34f), Quaternion.identity, PartRole.Tip));
                        }
                        AttachedTip(parts, true, "HealCore", new Vector3(0.26f, 0.27f, 0.25f));
                        break;
                    }
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 end = new Vector3(i * 0.5f, i == 0 ? 1.0f : 0.73f, 0f);
                        parts.Add(Link("HealBranch", stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                            Vector3.zero, end, stone ? 0.19f : 0.12f));
                        parts.Add(Part("HealSeed", round, end + Vector3.up * 0.19f,
                            new Vector3(0.3f, 0.43f, 0.27f), PartRole.Tip));
                    }
                    break;
                case HeadKind.GiftBoonDefence:
                    Crown(parts, stone, false);
                    break;
                case HeadKind.GiftBoonOffence:
                    Crown(parts, stone, true);
                    break;
                case HeadKind.GiftBane:
                    parts.Add(Part("BaneCrown", round, new Vector3(0f, 1.05f, 0f), new Vector3(0.66f, 0.36f, 0.6f)));
                    parts.Add(Link("BaneStem", stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                        Vector3.zero, Vector3.up * 0.95f, 0.17f));
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 3f;
                        Vector3 at = new Vector3(Mathf.Cos(angle) * 0.43f, 0.65f, Mathf.Sin(angle) * 0.43f);
                        parts.Add(Part("BanePendant", leaf, at, new Vector3(0.34f, 0.85f, 0.31f),
                            PartRole.Tip, new Vector3(0f, -angle * Mathf.Rad2Deg, 180f)));
                    }
                    break;
                case HeadKind.Ward:
                    parts.Add(Part("WardJoint", stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                        new Vector3(0f, 0.04f, 0f), new Vector3(0.25f, 0.23f, 0.25f)));
                    parts.Add(Part("WardSeed", round, new Vector3(0f, 0.53f, 0f), new Vector3(0.6f, 0.92f, 0.6f)));
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 3f;
                        parts.Add(Anchored("WardShell" + i, stone ? ShapeProfile.Shard(0.82f, -0.2f) : ShapeProfile.Leaf(-0.3f),
                            new Vector3(Mathf.Cos(angle) * 0.19f, 0.04f, Mathf.Sin(angle) * 0.19f),
                            new Vector3(0.43f, 1.2f, 0.34f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f)));
                    }
                    AttachedTip(parts, stone, "WardShell0", new Vector3(0.15f, 0.2f, 0.15f));
                    break;
                case HeadKind.Pulse:
                    parts.Add(Part("PulseStalk", stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                        new Vector3(0f, 0.21f, 0f), new Vector3(0.22f, 0.42f, 0.22f)));
                    parts.Add(Part("PulseCap", stone ? ShapeProfile.Block(0.16f) : ShapeProfile.Bulb(1.6f, 0.65f),
                        new Vector3(0f, 0.59f, 0f), new Vector3(1.35f, 0.45f, 1.05f)));
                    Tip(parts, stone, new Vector3(0f, 0.84f, 0f), new Vector3(0.16f, 0.13f, 0.16f));
                    break;
                case HeadKind.SelfTick:
                    Curl(parts, stone);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown head.");
            }
            return parts.ToArray();
        }

        static void Tip(List<LookPart> parts, bool stone, Vector3 at, Vector3 size,
            CountBand count = CountBand.One)
        {
            parts.Add(Part("Bud", stone ? ShapeProfile.Shard(0.62f, 0f) : ShapeProfile.Bulb(0.9f, 0.25f),
                at, size, PartRole.Tip, count: count));
        }

        static void AttachedTip(List<LookPart> parts, bool stone, string parent, Vector3 size)
        {
            parts.Add(Attached("Bud", stone ? ShapeProfile.Shard(0.62f, 0f) : ShapeProfile.Bulb(0.9f, 0.25f),
                parent, ShapeAnchor.Top, Vector3.down * 0.06f, size, PartRole.Tip));
        }

        static void Arch(List<LookPart> parts, bool stone)
        {
            if (stone)
            {
                parts.Add(Part("ArchPier", ShapeProfile.Block(0.28f, 0.16f, 0.12f),
                    new Vector3(-0.33f, 0.4f, 0f), new Vector3(0.55f, 0.88f, 0.58f),
                    euler: new Vector3(0f, 0f, -12f)));
                parts.Add(Part("ArchLintel", ShapeProfile.Block(0.12f, 0.05f, 0.08f, 0.55f),
                    new Vector3(0.18f, 0.98f, 0f), new Vector3(1.38f, 0.4f, 0.59f),
                    euler: new Vector3(0f, 0f, -9f)));
                parts.Add(Part("ArchKeystone", ShapeProfile.Block(), new Vector3(0.87f, 0.69f, 0f),
                    new Vector3(0.32f, 0.46f, 0.38f)));
                Tip(parts, true, new Vector3(0.89f, 0.31f, 0f), new Vector3(0.36f, 0.47f, 0.38f));
                parts.Add(Part("CanopyLintel", ShapeProfile.Block(0.12f), new Vector3(0.24f, 0.91f, 0.13f),
                    new Vector3(2.9f, 0.32f, 0.43f), count: CountBand.Many));
                for (int i = 0; i < 4; i++)
                {
                    CountBand band = i < 2 ? CountBand.Few : CountBand.Many;
                    float x = new[] { -0.32f, 0.28f, -0.92f, 1.5f }[i];
                    parts.Add(Link("PodBlock", ShapeProfile.Block(0.12f), new Vector3(x, 0.96f, 0.13f),
                        new Vector3(x, 0.7f, 0.13f), 0.22f, count: band));
                    Tip(parts, true, new Vector3(x, 0.48f, 0.13f), new Vector3(0.33f, 0.52f, 0.33f), band);
                }
                return;
            }

            Vector3[] curve =
            {
                Vector3.zero, new Vector3(-0.34f, 0.43f, 0.015f), new Vector3(-0.52f, 1.12f, -0.015f),
                new Vector3(-0.1f, 1.69f, -0.015f), new Vector3(0.53f, 1.63f, 0.015f), new Vector3(0.92f, 1.17f, 0.03f)
            };
            float[] widths = { 0.4f, 0.46f, 0.39f, 0.34f, 0.28f };
            for (int i = 1; i < curve.Length; i++)
            {
                Growth(parts, curve[i - 1], curve[i], widths[i - 1]);
            }
            Tip(parts, false, new Vector3(0.92f, 0.79f, 0.03f), new Vector3(0.43f, 0.7f, 0.37f));
            for (int i = 0; i < 4; i++)
            {
                CountBand band = i < 2 ? CountBand.Few : CountBand.Many;
                float x = new[] { -0.25f, 0.35f, -0.92f, 1.58f }[i];
                Vector3 start = new Vector3(i == 2 ? -0.1f : 0.53f, i == 2 ? 1.69f : 1.63f, 0.015f);
                Vector3 end = new Vector3(x, 1.18f + (i % 2 == 0 ? 0.05f : 0f), 0.1f);
                Growth(parts, start, end, 0.27f, band);
                Tip(parts, false, end + Vector3.down * 0.36f, new Vector3(0.34f, 0.64f, 0.31f), band);
            }
        }

        static void Fork(List<LookPart> parts, bool stone)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                string lobe = side < 0 ? "ForkLeft" : "ForkRight";
                if (stone)
                {
                    parts.Add(Anchored(lobe, ShapeProfile.Block(0.28f, 0.12f, 0.08f, 0.82f),
                        new Vector3(side * 0.49f, 0.015f, side * 0.025f),
                        new Vector3(side < 0 ? 0.7f : 0.66f, side < 0 ? 1.4f : 1.29f, 0.56f),
                        Quaternion.AngleAxis(-side * 7f, Vector3.forward)));
                    AttachedTip(parts, true, lobe, new Vector3(0.14f, 0.22f, 0.14f));
                    continue;
                }
                Vector3 fork = new Vector3(side * 0.55f, 0.42f, 0f);
                parts.Add(Link("ForkBranch", ShapeProfile.Segment(0.13f, 0.65f), Vector3.zero, fork, 0.25f));
                if (!stone)
                {
                    Joint(parts, fork, 0.24f);
                }
                Quaternion rotation = Quaternion.AngleAxis(-side * 14f, Vector3.forward)
                    * Quaternion.AngleAxis(side < 0 ? 0f : 180f, Vector3.up);
                parts.Add(Anchored(lobe, ShapeProfile.Leaf(1f, 1.1f),
                    fork, new Vector3(side < 0 ? 0.7f : 0.64f, side < 0 ? 1.95f : 1.72f, 0.38f), rotation));
                AttachedTip(parts, stone, lobe, new Vector3(0.14f, 0.22f, 0.14f));
            }
        }

        static void Crown(List<LookPart> parts, bool stone, bool offence)
        {
            parts.Add(Part("CrownBase", stone ? ShapeProfile.Block() : ShapeProfile.Bulb(),
                new Vector3(0f, 0.16f, 0f), new Vector3(0.5f, 0.3f, 0.46f)));
            int pieces = offence ? 6 : 4;
            for (int i = 0; i < pieces; i++)
            {
                if (offence)
                {
                    float angle = (i + 0.5f) * Mathf.PI * 2f / pieces;
                    parts.Add(Part("OffenceLoop", stone ? ShapeProfile.Block(0.1f) : ShapeProfile.Segment(0.1f, 0.3f),
                        new Vector3(Mathf.Cos(angle) * 0.58f, 0.88f + Mathf.Sin(angle) * 0.62f, 0f),
                        new Vector3(0.25f, 0.68f, stone ? 0.37f : 0.24f),
                        euler: new Vector3(0f, 0f, angle * Mathf.Rad2Deg)));
                }
                else
                {
                    float angle = i * Mathf.PI * 2f / pieces + Mathf.PI * 0.25f;
                    Vector3 at = new Vector3(Mathf.Cos(angle) * 0.2f, 0.2f, Mathf.Sin(angle) * 0.2f);
                    parts.Add(Anchored("DefencePlate", stone ? ShapeProfile.Shard(0.15f, 0f) : ShapeProfile.Leaf(0.52f, 0.62f),
                        at, new Vector3(stone ? 0.65f : 0.72f, stone ? 1.16f : 1.4f, 0.32f),
                        Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, -26f)));
                }
            }
            Tip(parts, stone, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.27f, 0.18f));
        }

        static void Curl(List<LookPart> parts, bool stone)
        {
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                float angle = (210f - i * 48f) * Mathf.Deg2Rad;
                Vector3 next = new Vector3(Mathf.Cos(angle) * 0.6f + 0.36f, 0.85f + Mathf.Sin(angle) * 0.64f, 0f);
                if (stone)
                {
                    parts.Add(Link("CurlBlock", ShapeProfile.Block(0.16f), previous, next, 0.34f));
                }
                else
                {
                    Growth(parts, previous, next, 0.28f);
                }
                previous = next;
            }
            Tip(parts, stone, previous + Vector3.down * 0.16f, new Vector3(0.32f, 0.41f, 0.3f));
        }
    }
}
