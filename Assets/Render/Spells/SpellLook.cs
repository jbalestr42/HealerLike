using System;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    [Serializable]
    public class SpellLook
    {
        public EffectElement element;
        // Picks the colour, mana elements keep their own
        public EffectFamily family;
        public EffectTempo tempo;
    }
}
