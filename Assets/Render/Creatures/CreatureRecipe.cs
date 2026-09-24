using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Stored by value in assets: append new members, never reorder or remove
    public enum Primitive
    {
        Sphere,
        Capsule,
        Cone,
        Torus,
        CylinderSegment,
        Leaf,
        Boulder,
        Pyramid,
        // A seeded stone from StoneVariants, picked by the part's variant
        Stone
    }

    [CreateAssetMenu(menuName = "Custom/Data/Render/CreatureRecipe")]
    public class CreatureRecipe : ScriptableObject
    {
        public CreaturePart[] parts = Array.Empty<CreaturePart>();
        public ArmDefinition[] arms = Array.Empty<ArmDefinition>();
        public RootDefinition roots = new RootDefinition
        {
            count = 4,
            segments = 3,
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

        // The body colour a wilting creature fades toward, plant green unless the recipe says otherwise
        public Color wiltColour = new Color(0.18f, 0.49f, 0.31f);
        public Vector3[] sourceLocal = Array.Empty<Vector3>();
        // Where the head sits on the body, zero lets the view take the middle of the sources
        public Vector3 neckLocal;
        // A stone mesh's second submesh, its ochre faces, draws in this colour
        public Color stoneOchre = new Color(0.7254902f, 0.6235294f, 0.427451f);
    }
}
