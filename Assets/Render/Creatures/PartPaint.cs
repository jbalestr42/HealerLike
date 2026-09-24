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
        readonly MaterialPropertyBlock _tipBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _ochreBlock = new MaterialPropertyBlock();

        // A tip carries its wider outline
        public void Paint(Renderer renderer, bool isTip, Color colour, float glow)
        {
            renderer.SetPropertyBlock(Block(isTip, colour, glow));
        }

        // A stone's ochre faces, its second submesh, take the ochre at the same brightness
        public void Paint(Renderer renderer, bool isTip, Color colour, Color ochre, float glow)
        {
            renderer.SetPropertyBlock(Block(isTip, colour, glow), 0);
            _ochreBlock.SetColor(RenderObjects.BaseColorId, PrimitiveMeshes.Brighten(ochre, glow));
            renderer.SetPropertyBlock(_ochreBlock, 1);
        }

        MaterialPropertyBlock Block(bool isTip, Color colour, float glow)
        {
            MaterialPropertyBlock block = _colourBlock;
            if (isTip)
            {
                block = _tipBlock;
                block.SetFloat(outlineWidthId, TipOutlineWidth);
            }

            block.SetColor(RenderObjects.BaseColorId, PrimitiveMeshes.Brighten(colour, glow));
            return block;
        }
    }
}
