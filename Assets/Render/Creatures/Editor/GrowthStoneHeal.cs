using System.Collections.Generic;
using UnityEngine;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneHeal
    {
        public static void Build(List<LookPart> parts, bool stone, ShapeProfile round)
        {
            if (stone)
            {
                parts.Add(
                    Anchored(
                        "HealPedestal",
                        ShapeProfile.Block(0.16f),
                        Vector3.zero,
                        new Vector3(0.92f, 0.38f, 0.72f),
                        Quaternion.identity
                    )
                );
                parts.Add(
                    Anchored(
                        "HealCore",
                        ShapeProfile.Block(0.22f),
                        new Vector3(0f, 0.3f, 0f),
                        new Vector3(0.51f, 0.54f, 0.47f),
                        Quaternion.identity
                    )
                );
                for (int side = -1; side <= 1; side += 2)
                {
                    parts.Add(
                        Anchored(
                            "HealSeed",
                            ShapeProfile.Block(0.22f),
                            new Vector3(side * 0.32f, 0.3f, 0f),
                            new Vector3(0.35f, 0.4f, 0.34f),
                            Quaternion.identity,
                            PartRole.Tip
                        )
                    );
                }

                AttachedTip(parts, true, "HealCore", new Vector3(0.26f, 0.27f, 0.25f));
                return;
            }

            for (int i = -1; i <= 1; i++)
            {
                Vector3 end = new Vector3(i * 0.5f, i == 0 ? 1.0f : 0.73f, 0f);
                parts.Add(Link("HealBranch", ShapeProfile.Segment(), Vector3.zero, end, 0.12f));
                parts.Add(
                    Part("HealSeed", round, end + Vector3.up * 0.19f, new Vector3(0.3f, 0.43f, 0.27f), PartRole.Tip)
                );
            }
        }
    }
}
