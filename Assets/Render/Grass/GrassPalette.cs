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

        // Blended by the tuft's soft patch lane, so the carpet varies slowly. The light one is the plant
        // base green, the other two step a sixth and a third of the way to the plant dark green
        public Color darkGreen = new Color(0.39f, 0.69f, 0.27f);
        public Color midGreen = new Color(0.45f, 0.74f, 0.26f);
        public Color lightGreen = new Color(0.5f, 0.79f, 0.25f);
        // The top fifth of every tuft, paler than the body as the painted tips are
        public Color tipGreen = new Color(0.66f, 0.87f, 0.42f);
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
