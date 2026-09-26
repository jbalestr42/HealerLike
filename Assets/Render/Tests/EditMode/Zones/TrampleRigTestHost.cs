using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class TrampleRigTestHost : ARigHost
    {
        public bool Build(CreatureRecipe recipe, Material material)
        {
            return BuildRig(recipe, transform, material, material, RenderTestAssets.LoadMeshes(), null, 1f);
        }
        public void Clear() { ReleaseRig(); }
        public override void OnHealthResolved(GameObject target, float value, bool critical) { }
    }
}
