using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneCrown
    {
        public static void Build(List<LookPart> parts, bool stone, bool offence)
        {
            parts.Add(
                Part(
                    "CrownBase",
                    stone ? ShapeProfile.Block() : ShapeProfile.Bulb(),
                    new Vector3(0f, 0.16f, 0f),
                    new Vector3(0.5f, 0.3f, 0.46f)
                )
            );
            int pieces = offence ? 6 : 4;
            for (int i = 0; i < pieces; i++)
            {
                if (offence)
                {
                    float angle = (i + 0.5f) * Mathf.PI * 2f / pieces;
                    parts.Add(
                        Part(
                            "OffenceLoop",
                            stone ? ShapeProfile.Block(0.1f) : ShapeProfile.Segment(0.1f, 0.3f),
                            new Vector3(Mathf.Cos(angle) * 0.58f, 0.88f + Mathf.Sin(angle) * 0.62f, 0f),
                            new Vector3(0.25f, 0.68f, stone ? 0.37f : 0.24f),
                            euler: new Vector3(0f, 0f, angle * Mathf.Rad2Deg)
                        )
                    );
                }
                else
                {
                    float angle = i * Mathf.PI * 2f / pieces + Mathf.PI * 0.25f;
                    Vector3 at = new Vector3(Mathf.Cos(angle) * 0.2f, 0.2f, Mathf.Sin(angle) * 0.2f);
                    parts.Add(
                        Anchored(
                            "DefencePlate",
                            stone
                                ? ShapeProfile.Block(0.2f, 0.25f, 0.08f, 0.72f, 0.55f)
                                : ShapeProfile.Leaf(0.35f, 0.62f, 0.38f),
                            at,
                            new Vector3(stone ? 0.65f : 0.72f, stone ? 1.16f : 1.4f, 0.32f),
                            Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, -26f)
                        )
                    );
                }
            }

            Tip(parts, stone, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.27f, 0.18f));
        }
    }
}
