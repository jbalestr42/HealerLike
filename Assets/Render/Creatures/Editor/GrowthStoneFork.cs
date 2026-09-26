using System.Collections.Generic;
using UnityEngine;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneFork
    {
        public static void Build(List<LookPart> parts, bool stone)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                string lobe = side < 0 ? "ForkLeft" : "ForkRight";
                if (stone)
                {
                    parts.Add(
                        Anchored(
                            lobe,
                            ShapeProfile.Block(0.2f, 0.1f, 0.1f, 0.82f, 0.72f),
                            new Vector3(side < 0 ? -0.54f : 0.52f, side < 0 ? 0f : 0.035f, side < 0 ? 0.04f : -0.06f),
                            new Vector3(side < 0 ? 0.82f : 0.74f, side < 0 ? 1.55f : 1.3f, side < 0 ? 0.72f : 0.67f),
                            Quaternion.Euler(0f, side < 0 ? -9f : 13f, side < 0 ? 3f : -11f)
                        )
                    );
                    AttachedTip(parts, true, lobe, new Vector3(0.14f, 0.22f, 0.14f));
                    continue;
                }

                Vector3 fork = new Vector3(side * 0.57f, 0.46f, 0f);
                parts.Add(Link("ForkBranch", ShapeProfile.Segment(0.13f, 0.8f), Vector3.zero, fork, 0.29f));
                Joint(parts, fork, 0.25f);
                Quaternion rotation =
                    Quaternion.AngleAxis(side * 6f, Vector3.forward)
                    * Quaternion.AngleAxis(side < 0 ? 0f : 180f, Vector3.up);
                parts.Add(
                    Anchored(
                        lobe,
                        ShapeProfile.Leaf(0.12f, 0.7f, -0.9f),
                        fork,
                        new Vector3(side < 0 ? 0.96f : 0.87f, side < 0 ? 2.02f : 1.84f, 0.42f),
                        rotation
                    )
                );
                AttachedTip(parts, false, lobe, new Vector3(0.14f, 0.22f, 0.14f));
            }
        }
    }
}
