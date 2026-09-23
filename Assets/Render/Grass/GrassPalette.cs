using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The tuft is the plant look material with the grass instancing keyword (GrassBlade.mat).
    // Each tuft takes one flat colour from these, in the greens of the plants, and its tip band the tip green.
    [Serializable]
    public class GrassPalette
    {
        public static readonly string InstancedKeyword = "HL_GRASS_INSTANCED";

        // Blended by the tuft's soft patch lane, so the carpet varies slowly. The sage body the reference
        // study sampled (#5b9055) and a step either side of it, all at 35 to 50 percent saturation
        public Color darkGreen = new Color(0.298f, 0.49f, 0.318f);
        public Color midGreen = new Color(0.357f, 0.565f, 0.333f);
        public Color lightGreen = new Color(0.38f, 0.604f, 0.361f);
        // The top fifth of every tuft, the study's pale tip (#8cba6c); the teal shade comes from the look's tint
        public Color tipGreen = new Color(0.549f, 0.729f, 0.424f);
        // Healed blades lean toward it, spikes take the slate
        public Color heal = new Color(0.78f, 0.95f, 0.29f);
        public Color slate = new Color(0.23f, 0.26f, 0.34f);

        // SetColor converts to the working colour space, as the plants' _BaseColor does
        public void Apply(MaterialPropertyBlock properties)
        {
            properties.SetColor("_HL_DarkGreen", darkGreen);
            properties.SetColor("_HL_MidGreen", midGreen);
            properties.SetColor("_HL_LightGreen", lightGreen);
            properties.SetColor("_HL_TipGreen", tipGreen);
            properties.SetColor("_HL_HealColor", heal);
            properties.SetColor("_HL_SlateColor", slate);
        }
    }
}
