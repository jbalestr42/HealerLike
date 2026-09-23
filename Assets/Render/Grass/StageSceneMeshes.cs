using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Grass
{
    // Runtime meshes for the stage scene, which has no render manager to hand out the baked ones. Removed in D2.
    public static class StageSceneMeshes
    {
        public static HLPrimitiveMeshes Create()
        {
            HLPrimitiveMeshes.Retain();
            HLPrimitiveMeshes meshes = ScriptableObject.CreateInstance<HLPrimitiveMeshes>();
            meshes.name = "StageSceneMeshes";
            meshes.sphere = HLPrimitiveMeshes.Get(HLPrimitive.Sphere);
            meshes.capsule = HLPrimitiveMeshes.Get(HLPrimitive.Capsule);
            meshes.cone = HLPrimitiveMeshes.Get(HLPrimitive.Cone);
            meshes.cylinder = HLPrimitiveMeshes.Get(HLPrimitive.CylinderSegment);
            meshes.bladeCone = HLPrimitiveMeshes.Get(HLPrimitive.Cone, HLGrassField.BladeSides, 2);
            meshes.annulus = HLGrassRing.CreateAnnulus();
            return meshes;
        }

        public static void Release(HLPrimitiveMeshes meshes)
        {
            Object.Destroy(meshes.annulus);
            Object.Destroy(meshes);
            HLPrimitiveMeshes.Release();
        }
    }
}
