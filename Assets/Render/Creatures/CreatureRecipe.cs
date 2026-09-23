using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public enum Primitive
    {
        Sphere,
        Capsule,
        Cone,
        Torus,
        CylinderSegment
    }

    [CreateAssetMenu(menuName = "Custom/Data/Render/CreatureRecipe")]
    public class CreatureRecipe : ScriptableObject
    {
        public Part[] parts = Array.Empty<Part>();
        public ArmDefinition[] arms = Array.Empty<ArmDefinition>();
        public RootDefinition roots = new RootDefinition
        {
            count = 4,
            footRadius = 0.38f,
            hipHeight = 0.18f,
            kneeHeight = 0.09f,
            thickness = 0.022f,
            colour = new Color(0.18f, 0.49f, 0.31f)
        };
        public IdleDefinition idle = new IdleDefinition
        {
            swayDegrees = 2.5f,
            swayFrequency = 0.12f,
            breathAmount = 0.025f,
            breathFrequency = 0.25f,
            seed = 17
        };

        // Socket hints only guide new authoring, the runtime never relocates authored sockets
        public Vector3 targetLocal;
        public Vector3[] sourceLocal = Array.Empty<Vector3>();
    }
}
