using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Accepted recipe values belong to the assembly, so an invalid edit cannot change its live pose.
    public class CreatureRigData
    {
        public readonly CreaturePart[] parts;
        public readonly ArmDefinition[] arms;
        public readonly RootDefinition roots;
        public readonly IdleDefinition idle;
        public readonly Vector3[] sourceLocal;
        public readonly Vector3 neckLocal;
        public readonly Color wiltColour;
        public readonly Color stoneOchre;

        public CreatureRigData(CreatureRecipe recipe)
        {
            parts = (CreaturePart[])recipe.parts.Clone();
            arms = (ArmDefinition[])recipe.arms.Clone();
            for (int i = 0; i < arms.Length; i++)
            {
                arms[i].restJoints = (Vector3[])arms[i].restJoints.Clone();
            }

            roots = recipe.roots;
            idle = recipe.idle;
            sourceLocal = (Vector3[])recipe.sourceLocal.Clone();
            neckLocal = recipe.neckLocal;
            wiltColour = recipe.wiltColour;
            stoneOchre = recipe.stoneOchre;
        }
    }
}
