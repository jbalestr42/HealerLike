using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The blade is the plant look material with the grass instancing keyword (GrassBlade.mat).
    // Each blade takes one flat colour from these, in the greens of the plants.
    [Serializable]
    public class GrassPalette
    {
        public static readonly string InstancedKeyword = "HL_GRASS_INSTANCED";

        // Picked by the blade's patch lane, so neighbouring cells share a green. The light one is the plant
        // base green, the other two step a sixth and a third of the way to the plant dark green
        public Color darkGreen = new Color(0.39f, 0.69f, 0.27f);
        public Color midGreen = new Color(0.45f, 0.74f, 0.26f);
        public Color lightGreen = new Color(0.5f, 0.79f, 0.25f);
        // Healed blades lean toward it, spikes take the slate
        public Color heal = new Color(0.78f, 0.95f, 0.29f);
        public Color slate = new Color(0.23f, 0.26f, 0.34f);

        // SetColor converts to the working colour space, as the plants' _BaseColor does
        public void Apply(MaterialPropertyBlock properties)
        {
            properties.SetColor("_HL_DarkGreen", darkGreen);
            properties.SetColor("_HL_MidGreen", midGreen);
            properties.SetColor("_HL_LightGreen", lightGreen);
            properties.SetColor("_HL_HealColor", heal);
            properties.SetColor("_HL_SlateColor", slate);
        }
    }
}
