using System;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Spells
{
    // One element: its shape parts, its motion and its socket. Parts anchored on a unit are in body radii,
    // Link parts in world units and Ground parts in area radii.
    [Serializable]
    public class ElementEntry
    {
        // Body role parts are the shape, Stem role parts are the stalks of the shape parts in the same order
        public LookPart[] parts = Array.Empty<LookPart>();
        // One bead per stack
        public LookPart[] stackBeads = Array.Empty<LookPart>();
        public LookPart[] criticalRings = Array.Empty<LookPart>();
        // Shows the caster's side
        public LookPart[] sideRim = Array.Empty<LookPart>();
        public EffectMotionKind motion;
        public EffectSocket socket;
        public EffectCount count;
        // The fewest shape parts shown, one stack or charge adds one more up to every part
        public int minCount = 1;
        public float cycleSeconds = 0.6f;
    }
}
