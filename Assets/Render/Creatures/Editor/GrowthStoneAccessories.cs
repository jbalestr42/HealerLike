using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneAccessories
    {
        public static LookVocabulary.AccessoryEntry Build(AccessoryKind kind)
        {
            return new LookVocabulary.AccessoryEntry
            {
                socket = kind == AccessoryKind.ShardBarbs ? AccessorySocket.HipOrbit : AccessorySocket.NeckOrbit,
                isCentered = kind == AccessoryKind.TierRings || kind == AccessoryKind.SmallTorus
                    || kind == AccessoryKind.ThornCollar || kind == AccessoryKind.ConeCrown
                    || kind == AccessoryKind.TwinSeeds || kind == AccessoryKind.ShardBarbs,
                miniHeadAt = new Vector3(1.75f, 0.42f, 0f), miniHeadScale = 0.46f,
                plant = Build(kind, false), stone = Build(kind, true)
            };
        }

        static LookPart[] Build(AccessoryKind kind, bool stone)
        {
            List<LookPart> parts = new List<LookPart>();
            ShapeProfile seed = stone ? ShapeProfile.Block(0.2f) : ShapeProfile.Bulb(1f, 0.16f);
            ShapeProfile stalk = stone ? ShapeProfile.Block(0.1f) : ShapeProfile.Segment(0.22f, 0.3f);
            ShapeProfile thorn = stone ? ShapeProfile.Shard(0.88f, 0f) : ShapeProfile.Leaf(0.1f, 0.55f);
            switch (kind)
            {
                case AccessoryKind.MiniHead:
                    parts.Add(Link("Offshoot", stalk, Vector3.zero, new Vector3(1.75f, 0.42f, 0f), 0.14f));
                    parts.Add(Part("OffshootJoint", seed, new Vector3(1.75f, 0.42f, 0f), Vector3.one * 0.2f));
                    break;
                case AccessoryKind.Hook:
                    parts.Add(Link("HookStem", stalk, Vector3.zero, new Vector3(1.0f, 0.08f, 0f), 0.16f));
                    parts.Add(Link("HookBend", stalk, new Vector3(1.0f, 0.08f, 0f), new Vector3(1.32f, 0.48f, 0f), 0.2f));
                    parts.Add(Part("HookTip", thorn, new Vector3(1.28f, 0.73f, 0f), new Vector3(0.25f, 0.49f, 0.22f),
                        euler: new Vector3(0f, 0f, 30f)));
                    break;
                case AccessoryKind.Antenna:
                    parts.Add(Link("Antenna", stalk, Vector3.zero, new Vector3(1.14f, 1.18f, 0f), 0.13f));
                    parts.Add(Part("AntennaSeed", seed, new Vector3(1.14f, 1.26f, 0f), new Vector3(0.25f, 0.35f, 0.24f)));
                    break;
                case AccessoryKind.ThornCollar:
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 6f;
                        Vector3 from = new Vector3(Mathf.Cos(angle) * 0.17f, -0.12f, Mathf.Sin(angle) * 0.17f);
                        Vector3 to = new Vector3(Mathf.Cos(angle) * 0.75f, -0.52f, Mathf.Sin(angle) * 0.75f);
                        parts.Add(Link("Thorn", thorn, from, to, 0.19f));
                    }
                    break;
                case AccessoryKind.TierRings:
                    for (int i = 0; i < 3; i++)
                    {
                        parts.Add(Part("TierRing", ShapeProfile.Ring(0.24f, stone),
                            new Vector3(0f, -0.12f - i * 0.24f, 0f), new Vector3(0.94f - i * 0.1f, 0.14f, 0.74f)));
                    }
                    break;
                case AccessoryKind.TwinSeeds:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        parts.Add(Link("TwinSeedStem", stalk, Vector3.zero, new Vector3(side * 0.43f, -0.32f, 0f), 0.12f));
                        parts.Add(Part("TwinSeed", seed, new Vector3(side * 0.51f, -0.51f, 0f),
                            new Vector3(0.34f, 0.66f, 0.35f), euler: new Vector3(0f, 0f, -side * 22f)));
                    }
                    break;
                case AccessoryKind.StalkBeads:
                    parts.Add(Link("BeadStalk", stalk, Vector3.zero, new Vector3(1.0f, 0.9f, 0f), 0.1f));
                    for (int i = 0; i < 3; i++)
                    {
                        parts.Add(Part("StalkBead", seed, new Vector3(0.82f + i * 0.11f, 0.3f + i * 0.29f, 0f),
                            Vector3.one * (0.27f - i * 0.03f)));
                    }
                    break;
                case AccessoryKind.SmallTorus:
                    parts.Add(Part("SmallRing", ShapeProfile.Ring(0.23f, stone), new Vector3(0f, -0.2f, 0f),
                        new Vector3(1.05f, 0.19f, 0.89f)));
                    break;
                case AccessoryKind.ConeCrown:
                    for (int i = 0; i < 3; i++)
                    {
                        parts.Add(Part("CrownCone", thorn, new Vector3((i - 1) * 0.25f, 1.08f, 0f),
                            new Vector3(0.22f, i == 1 ? 0.58f : 0.44f, 0.22f)));
                    }
                    break;
                case AccessoryKind.DripBeads:
                    parts.Add(Link("DripBeam", stalk, new Vector3(0.05f, 0.3f, 0f), new Vector3(1.43f, 0.38f, 0f), 0.15f));
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 top = new Vector3(0.65f + i * 0.37f, 0.34f, 0f);
                        Vector3 end = top + Vector3.down * (i == 1 ? 0.58f : 0.32f);
                        parts.Add(Link("DripStem", stalk, top, end, 0.09f));
                        parts.Add(Part("DripSeed", seed, end + Vector3.down * 0.14f, new Vector3(0.22f, 0.34f, 0.2f)));
                    }
                    break;
                case AccessoryKind.ShardBarbs:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        parts.Add(Part("ShardBarb", stone ? ShapeProfile.Shard() : ShapeProfile.Leaf(0.2f, 0.55f),
                            new Vector3(side * 0.48f, 0.26f, 0f), new Vector3(0.3f, 0.98f, 0.26f),
                            euler: new Vector3(0f, side < 0 ? 180f : 0f, -side * 24f)));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "No accessory fragment.");
            }
            for (int i = 0; i < parts.Count; i++)
            {
                LookPart part = parts[i];
                part.role = PartRole.Accessory;
                part.colour = ColourRole.Body;
                part.glow = 0f;
                parts[i] = part;
            }
            return parts.ToArray();
        }
    }
}
