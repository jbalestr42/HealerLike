using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Everything a SpellEffect needs to build and move one element
    [System.Serializable]
    public class EffectRecipe
    {
        public EffectRecipe[] additions = System.Array.Empty<EffectRecipe>();
        public EffectChannels channels;
        public EffectPresentation presentation => entry != null ? entry.presentation : null;
        public EffectElement element;
        public ElementEntry entry;
        public EffectMotionKind motion;
        public EffectSocket socket;
        public EffectFamily family;
        public EffectTempo tempo;
        // One motion cycle: the period of a ticking handler, else the entry's own cycle
        public float cycleSeconds;
        public Color colour;
        public int count;
        // The element's size on its socket, a harder hit draws a bigger burst
        public float scale = 1f;
        public LookPalette palette;

        // Resolve legacy unspecified clocks per instance without changing a shared authored recipe.
        public EffectRecipe ResolveCycle()
        {
            if (cycleSeconds > 0f)
            {
                return this;
            }
            EffectRecipe resolved = (EffectRecipe)MemberwiseClone();
            resolved.cycleSeconds = entry.cycleSeconds;
            return resolved;
        }
    }
}
