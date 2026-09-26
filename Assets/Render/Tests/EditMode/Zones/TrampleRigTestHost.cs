using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    // This helper belongs to the Editor-only test assembly and must be attachable during EditMode tests.
    [ExecuteAlways]
    public class TrampleRigTestHost : ARigHost
    {
        public bool Build(CreatureRecipe recipe, Material material)
        {
            return BuildRig(recipe, transform, material, material, RenderTestAssets.LoadMeshes(), null, 1f);
        }
        public void Clear()
        {
            ReleaseRig();
        }

        public override void OnHealthResolved(GameObject target, float value, bool critical)
        {
        }
    }
}
