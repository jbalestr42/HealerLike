using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Everything a SpellEffect needs to build and move one element
    public class EffectRecipe
    {
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
    }
}
