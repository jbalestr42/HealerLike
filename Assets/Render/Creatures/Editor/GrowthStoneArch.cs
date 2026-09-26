using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneArch
    {
        public static void Build(List<LookPart> parts, bool stone)
        {
            if (stone)
            {
                parts.Add(
                    Part(
                        "ArchPier",
                        ShapeProfile.Block(0.23f, 0.08f, 0.12f, 0.8f, 0.72f),
                        new Vector3(-0.36f, 0.46f, 0.015f),
                        new Vector3(0.8f, 1.13f, 0.72f),
                        euler: new Vector3(0f, -7f, -13f)
                    )
                );
                parts.Add(
                    Part(
                        "ArchLintel",
                        ShapeProfile.Block(0.16f, 0.03f, 0.1f, 0.55f, 0.18f),
                        new Vector3(0.2f, 1.18f, -0.02f),
                        new Vector3(1.55f, 0.53f, 0.76f),
                        euler: new Vector3(0f, 4f, -10f)
                    )
                );
                parts.Add(
                    Part(
                        "ArchKeystone",
                        ShapeProfile.Block(0.2f, 0.12f, 0.1f, 0.65f, 0.42f),
                        new Vector3(0.99f, 0.82f, 0.01f),
                        new Vector3(0.42f, 0.59f, 0.46f)
                    )
                );
                parts.Add(
                    Part(
                        "Bud",
                        ShapeProfile.Block(0.19f, 0.13f, 0.1f, 0.65f, 0.48f),
                        new Vector3(1f, 0.35f, 0.02f),
                        new Vector3(0.44f, 0.54f, 0.45f),
                        PartRole.Tip
                    )
                );
                // Count organs clear the broad pier and the original hanging block. Extend the mineral
                // canopy through connected slabs, rather than hiding an extra pod directly behind the pier.
                float[] podX = { -1.85f, 2f, -2.85f, 2.95f };
                for (int i = 0; i < 4; i++)
                {
                    CountBand band = i < 2 ? CountBand.Few : CountBand.Many;
                    float x = podX[i];
                    float fromX = i < 2 ? (i == 0 ? -0.36f : 0.86f) : podX[i - 2];
                    Vector3 top = new Vector3(x, 1.12f, 0.02f);
                    parts.Add(
                        Link(
                            "CanopySlab",
                            ShapeProfile.Block(0.12f),
                            new Vector3(fromX, 1.12f, 0.02f),
                            top,
                            0.29f,
                            count: band
                        )
                    );
                    parts.Add(
                        Link(
                            "PodBlock",
                            ShapeProfile.Block(0.12f),
                            top,
                            new Vector3(x, 0.7f, 0.02f),
                            0.22f,
                            count: band
                        )
                    );
                    parts.Add(
                        Part(
                            "Bud",
                            ShapeProfile.Block(0.19f, 0.13f, 0.1f, 0.65f, 0.48f),
                            new Vector3(x, 0.43f, 0.02f),
                            new Vector3(0.37f, 0.58f, 0.37f),
                            PartRole.Tip,
                            count: band
                        )
                    );
                }

                return;
            }

            Vector3[] curve =
            {
                Vector3.zero,
                new Vector3(-0.32f, 0.5f, 0.015f),
                new Vector3(-0.43f, 1.28f, -0.015f),
                new Vector3(0.04f, 1.94f, -0.015f),
                new Vector3(0.77f, 1.85f, 0.015f),
                new Vector3(1.13f, 1.32f, 0.03f),
            };
            float[] widths = { 0.47f, 0.53f, 0.45f, 0.38f, 0.31f };
            for (int i = 1; i < curve.Length; i++)
            {
                Growth(parts, curve[i - 1], curve[i], widths[i - 1]);
            }

            Tip(parts, false, new Vector3(1.13f, 0.92f, 0.03f), new Vector3(0.5f, 0.78f, 0.43f));
            Vector3[] podEnds =
            {
                new Vector3(-1.4f, 1.23f, 0.03f),
                new Vector3(2.05f, 1.18f, 0.03f),
                new Vector3(-2.3f, 1.23f, 0.03f),
                new Vector3(2.95f, 1.18f, 0.03f),
            };
            for (int i = 0; i < 4; i++)
            {
                CountBand band = i < 2 ? CountBand.Few : CountBand.Many;
                Vector3 start = i < 2 ? curve[i == 0 ? 3 : 4] : podEnds[i - 2];
                Vector3 end = podEnds[i];
                Growth(parts, start, end, 0.27f, band);
                Tip(parts, false, end + Vector3.down * 0.36f, new Vector3(0.34f, 0.64f, 0.31f), band);
            }
        }
    }
}
