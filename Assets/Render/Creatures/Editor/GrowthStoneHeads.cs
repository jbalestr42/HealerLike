using System.Collections.Generic;
using System;
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
                plant = Build(kind, false),
                stone = Build(kind, true),
                carriesCount = kind == HeadKind.Arch,
                plantStemScale = NeckScale(kind),
                plantStem =
                    kind == HeadKind.Arch
                        ? new LookVocabulary.PlantStemEntry
                        {
                            segments = 2,
                            thicknessScale = 2.8f,
                            bow = 0.12f,
                            jointScale = 0.76f,
                            segmentShape = ShapeProfile.Segment(0.12f, 0.85f),
                            jointShape = ShapeProfile.Bulb(),
                        }
                        : null,
            };
        }

        static float NeckScale(HeadKind kind)
        {
            switch (kind)
            {
                case HeadKind.Spear:
                    return 0.95f;
                case HeadKind.Arch:
                    return 0.6f;
                case HeadKind.Fork:
                case HeadKind.GiftBoonDefence:
                    return 0.25f;
                case HeadKind.Conductor:
                    return 0.35f;
                case HeadKind.GiftHeal:
                    return 0.7f;
                case HeadKind.GiftBane:
                    return 0.55f;
                case HeadKind.SelfTick:
                    return 0.6f;
                default:
                    return 0.45f;
            }
        }

        static LookPart[] Build(HeadKind kind, bool stone)
        {
            List<LookPart> parts = new List<LookPart>();
            ShapeProfile round = stone ? ShapeProfile.Block(0.25f, 0.12f, 0.13f, 0.74f, 0.65f) : ShapeProfile.Bulb();
            ShapeProfile leaf = stone ? ShapeProfile.Shard(0.82f, 0.04f) : ShapeProfile.Leaf(0.35f, 0.82f);
            if (stone)
            {
                leaf.ridge = 0.45f;
            }

            switch (kind)
            {
                case HeadKind.Bud:
                    parts.Add(
                        Anchored(
                            "BudBody",
                            round,
                            new Vector3(0f, 0.01f, 0f),
                            new Vector3(1.17f, 1.12f, 1f),
                            Quaternion.identity
                        )
                    );
                    AttachedTip(parts, stone, "BudBody", new Vector3(0.14f, 0.22f, 0.14f));
                    break;
                case HeadKind.Spear:
                    parts.Add(Part("SpearJoint", round, new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 0.2f)));
                    ShapeProfile lance = stone ? leaf : ShapeProfile.Leaf(0.2f, 0.65f);
                    if (!stone)
                    {
                        lance.taper = 0.78f;
                    }

                    parts.Add(
                        Anchored(
                            "SpearBlade",
                            lance,
                            new Vector3(0f, 0.21f, 0f),
                            stone ? new Vector3(0.5f, 1.35f, 0.36f) : new Vector3(0.48f, 1.8f, 0.31f),
                            Quaternion.identity
                        )
                    );
                    AttachedTip(parts, stone, "SpearBlade", new Vector3(0.11f, 0.2f, 0.11f));
                    break;
                case HeadKind.Arch:
                    GrowthStoneArch.Build(parts, stone);
                    break;
                case HeadKind.Conductor:
                    parts.Add(
                        Part(
                            "ConductorCollar",
                            ShapeProfile.Ring(0.23f, stone),
                            new Vector3(0f, 0.28f, 0f),
                            new Vector3(1.55f, 0.3f, 1.18f)
                        )
                    );
                    parts.Add(
                        Part(
                            "ConductorMast",
                            stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                            new Vector3(0f, 0.53f, 0f),
                            new Vector3(0.24f, 1.02f, 0.24f)
                        )
                    );
                    parts.Add(
                        Part("ConductorLower", round, new Vector3(0f, 0.74f, 0f), new Vector3(0.62f, 0.58f, 0.54f))
                    );
                    parts.Add(
                        Part("ConductorUpper", round, new Vector3(-0.035f, 1.22f, 0f), new Vector3(0.39f, 0.44f, 0.36f))
                    );
                    AttachedTip(parts, stone, "ConductorUpper", new Vector3(0.14f, 0.19f, 0.14f));
                    break;
                case HeadKind.Fork:
                    GrowthStoneFork.Build(parts, stone);
                    break;
                case HeadKind.GiftHeal:
                    GrowthStoneHeal.Build(parts, stone, round);
                    break;
                case HeadKind.GiftBoonDefence:
                    GrowthStoneCrown.Build(parts, stone, false);
                    break;
                case HeadKind.GiftBoonOffence:
                    GrowthStoneCrown.Build(parts, stone, true);
                    break;
                case HeadKind.GiftBane:
                    parts.Add(Part("BaneCrown", round, new Vector3(0f, 1.05f, 0f), new Vector3(0.66f, 0.36f, 0.6f)));
                    parts.Add(
                        Link(
                            "BaneStem",
                            stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                            Vector3.zero,
                            Vector3.up * 0.95f,
                            0.17f
                        )
                    );
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 3f;
                        Vector3 at = new Vector3(Mathf.Cos(angle) * 0.43f, 0.65f, Mathf.Sin(angle) * 0.43f);
                        parts.Add(
                            Part(
                                "BanePendant",
                                leaf,
                                at,
                                new Vector3(0.34f, 0.85f, 0.31f),
                                PartRole.Tip,
                                new Vector3(0f, -angle * Mathf.Rad2Deg, 180f)
                            )
                        );
                    }

                    break;
                case HeadKind.Ward:
                    parts.Add(
                        Part(
                            "WardJoint",
                            stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                            new Vector3(0f, 0.04f, 0f),
                            new Vector3(0.25f, 0.23f, 0.25f)
                        )
                    );
                    parts.Add(Part("WardSeed", round, new Vector3(0f, 0.53f, 0f), new Vector3(0.6f, 0.92f, 0.6f)));
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 3f;
                        parts.Add(
                            Anchored(
                                "WardShell" + i,
                                stone ? ShapeProfile.Shard(0.82f, -0.2f) : ShapeProfile.Leaf(-0.3f),
                                new Vector3(Mathf.Cos(angle) * 0.19f, 0.04f, Mathf.Sin(angle) * 0.19f),
                                new Vector3(0.43f, 1.2f, 0.34f),
                                Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f)
                            )
                        );
                    }

                    AttachedTip(parts, stone, "WardShell0", new Vector3(0.15f, 0.2f, 0.15f));
                    break;
                case HeadKind.Pulse:
                    parts.Add(
                        Part(
                            "PulseStalk",
                            stone ? ShapeProfile.Block() : ShapeProfile.Segment(),
                            new Vector3(0f, 0.21f, 0f),
                            new Vector3(0.22f, 0.42f, 0.22f)
                        )
                    );
                    parts.Add(
                        Part(
                            "PulseCap",
                            stone ? ShapeProfile.Block(0.16f) : ShapeProfile.Bulb(1.6f, 0.65f),
                            new Vector3(0f, 0.59f, 0f),
                            new Vector3(1.35f, 0.45f, 1.05f)
                        )
                    );
                    Tip(parts, stone, new Vector3(0f, 0.84f, 0f), new Vector3(0.16f, 0.13f, 0.16f));
                    break;
                case HeadKind.SelfTick:
                    GrowthStoneCurl.Build(parts, stone);
                    break;
                default:
                    Debug.LogError("[GrowthStoneHeads] Unknown head.");
                    return Array.Empty<LookPart>();
            }

            return parts.ToArray();
        }
    }
}
