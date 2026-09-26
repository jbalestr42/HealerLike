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
        Stone,
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
            colour = Color.white,
        };
        public IdleDefinition idle = new IdleDefinition
        {
            swayDegrees = 2.5f,
            swayFrequency = 0.12f,
            breathAmount = 0.025f,
            breathFrequency = 0.25f,
            seed = 17,
        };

        // The body colour a wilting creature fades toward. The colours here are the palette's, set by the
        // composer or the authoring; white shows a field left unset.
        public Color wiltColour = Color.white;
        public Vector3[] sourceLocal = Array.Empty<Vector3>();

        // Where the head sits on the body, zero lets the view take the middle of the sources
        public Vector3 neckLocal;

        // A stone mesh's second submesh, its ochre faces, draws in this colour
        public Color stoneOchre = Color.white;
    }
}
