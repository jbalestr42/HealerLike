using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // A plant's lianas at rest: four coils of 48 half-cell links from the neck reach across the 16-cell board
    public static class LianaShape
    {
        // The lianas leave the neck this far to either side, in body units
        static readonly float armSpread = 0.15f;

        // One liana coils four turns of this many links, twelve to a turn, drifting forward a little each link so the
        // coil does not close on itself
        static readonly int armLinks = 48;
        static readonly int armLinksPerTurn = 12;
        static readonly float armLinkLength = 0.5f;
        static readonly float armDrift = 0.015f;
        static readonly float armRadius = 0.045f;

        public static void Arms(
            CreatureRecipe recipe,
            Vector3 neck,
            int armCount,
            float bodyUnit,
            Color colour,
            Color accent
        )
        {
            Vector3 bodyPivot = recipe.parts[0].localPosition;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new ArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                // The arms leave the neck on either side, the first on the left
                float side = armSpread;
                if (j % 2 == 0)
                {
                    side = -armSpread;
                }

                recipe.sourceLocal[j] = (neck + Vector3.right * side) * bodyUnit;
                recipe.arms[j] = Arm(recipe.sourceLocal[j] - bodyPivot, colour, accent);
            }
        }

        // One liana from its root on the body
        public static ArmDefinition Arm(Vector3 rootLocal, Color colour, Color tipColour)
        {
            Vector3[] rest = new Vector3[armLinks + 1];
            for (int i = 0; i < armLinks; i++)
            {
                float angle = i * Mathf.PI * 2f / armLinksPerTurn;
                Vector3 link = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), armDrift).normalized;
                rest[i + 1] = rest[i] + link * armLinkLength;
            }

            return new ArmDefinition
            {
                bodyPart = 0,
                rootLocal = rootLocal,
                segmentCount = armLinks,
                segmentLength = armLinkLength,
                radius = armRadius,
                restJoints = rest,
                bendPole = Vector3.up,
                colour = colour,
                tipColour = tipColour,
            };
        }
    }
}
