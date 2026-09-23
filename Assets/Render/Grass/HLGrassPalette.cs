using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The blade is the plant look material with the grass instancing keyword (GrassBlade.mat) and the grass colours
    public static class HLGrassPalette
    {
        public static readonly string InstancedKeyword = "HL_GRASS_INSTANCED";

        public static void Apply(MaterialPropertyBlock properties)
        {
            SetColor(properties, "_HL_RootColor", 43, 110, 87);
            SetColor(properties, "_HL_MidColor", 101, 159, 89);
            SetColor(properties, "_HL_TipColor", 169, 204, 96);
            SetColor(properties, "_HL_HealColor", 198, 242, 74);
            SetColor(properties, "_HL_SlateRoot", 58, 66, 87);
            SetColor(properties, "_HL_SlateTip", 74, 84, 104);
        }

        static void SetColor(MaterialPropertyBlock properties, string name, byte red, byte green, byte blue)
        {
            Color color = new Color32(red, green, blue, 255);
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                color = color.linear;
            }

            properties.SetVector(name, color);
        }
    }
}
