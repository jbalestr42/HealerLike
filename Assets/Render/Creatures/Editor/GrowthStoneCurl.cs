using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    public static class GrowthStoneCurl
    {
        public static void Build(List<LookPart> parts, bool stone)
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
