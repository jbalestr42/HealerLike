using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Colours one part's renderer through property blocks, so every unit shares its material
    public class PartPaint
    {
        // Tips draw a wider outline, so a coral tip on a green body separates by an edge and not only by hue
        public static readonly float TipOutlineWidth = 1.5f;
        static readonly int outlineWidthId = Shader.PropertyToID("_HLOutlineWidthMultiplier");
        readonly MaterialPropertyBlock _colourBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _ochreBlock = new MaterialPropertyBlock();
        readonly List<Material> _materials = new List<Material>(2);

        public CreatureSelection selection { get; set; }

        // Indexed blocks override renderer-wide blocks and survive a material layout change.
        public void Clear(Renderer renderer)
        {
            int count = renderer.sharedMaterials.Length;
            for (int i = 0; i < count; i++)
            {
                renderer.SetPropertyBlock(null, i);
            }

            renderer.SetPropertyBlock(null);
        }

        // A tip carries its wider outline
        public void Paint(Renderer renderer, bool isTip, Color colour, float glow)
        {
            renderer.SetPropertyBlock(Block(isTip, colour, glow, renderer.sharedMaterial));
        }

        // A stone's ochre faces, its second submesh, take the ochre at the same brightness
        public void Paint(Renderer renderer, bool isTip, Color colour, Color ochre, float glow)
        {
            renderer.GetSharedMaterials(_materials);
            renderer.SetPropertyBlock(Block(isTip, colour, glow, MaterialAt(0)), 0);
            Fill(_ochreBlock, false, ochre, glow, MaterialAt(1));
            renderer.SetPropertyBlock(_ochreBlock, 1);
        }

        MaterialPropertyBlock Block(bool isTip, Color colour, float glow, Material material)
        {
            Fill(_colourBlock, isTip, colour, glow, material);
            return _colourBlock;
        }

        Material MaterialAt(int index)
        {
            return index < _materials.Count ? _materials[index] : null;
        }

        static float AuthoredWidth(Material material)
        {
            if (!material || !material.HasProperty(outlineWidthId))
            {
                return 1f;
            }

            float width = material.GetFloat(outlineWidthId);
            return float.IsFinite(width) ? Mathf.Max(0f, width) : 1f;
        }

        void Fill(MaterialPropertyBlock block, bool isTip, Color colour, float glow, Material material)
        {
            block.Clear();
            float width = selection.Width(isTip ? TipOutlineWidth : AuthoredWidth(material));
            if (isTip || selection.isHighlighted)
            {
                block.SetFloat(outlineWidthId, width);
            }

            block.SetColor(RenderObjects.BaseColorId, selection.Tint(PrimitiveMeshes.Brighten(colour, glow)));
        }
    }
}
