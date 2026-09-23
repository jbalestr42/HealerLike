using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // What an effect draws, the composer picks one from the family and the group of a handler
    public enum EffectElement
    {
        Burst,
        Rise,
        Stalks,
        Drips,
        Orbit,
        Plates,
        Bud,
        Press,
        Crack,
        ManaUp,
        ManaDown,
        Beam,
        Ring,
        Litter
    }

    // How the parts of an element move, one motion cycle at a time
    public enum EffectMotion
    {
        Burst,
        Rise,
        Grow,
        Fall,
        Orbit,
        Close,
        Press,
        Shed
    }

    // Where an element sits, read from the anchors of its target
    public enum EffectSocket
    {
        Body,
        AboveHead,
        UnderHead,
        Feet,
        Link,
        Ground
    }

    // What decides how many shape parts of an element show
    public enum EffectCount
    {
        Fixed,
        Stacks,
        Charges,
        Amount
    }

    // One element: its shape parts, its motion and its socket. Parts anchored on a unit are in body radii,
    // Link parts in world units and Ground parts in area radii.
    [Serializable]
    public class ElementEntry
    {
        // Body role parts are the shape, Stem role parts are the stalks of the shape parts in the same order
        public LookPart[] parts = new LookPart[0];
        // One bead per stack
        public LookPart[] stackBeads = new LookPart[0];
        public LookPart[] criticalRings = new LookPart[0];
        // Shows the caster's side
        public LookPart[] sideRim = new LookPart[0];
        public EffectMotion motion;
        public EffectSocket socket;
        public EffectCount count;
        // The fewest shape parts shown, one stack or charge adds one more up to every part
        public int minCount = 1;
        public float cycleSeconds = 0.6f;
    }

    [CreateAssetMenu(menuName = "Custom/Data/Render/EffectVocabulary")]
    public class EffectVocabulary : SerializedScriptableObject
    {
        public LookPalette palette;

        [DictionaryDrawerSettings(KeyLabel = "Element", ValueLabel = "Entry")]
        public Dictionary<EffectElement, ElementEntry> elements = new Dictionary<EffectElement, ElementEntry>();

        public ElementEntry GetEntry(EffectElement element)
        {
            if (elements == null || !elements.ContainsKey(element))
            {
                Debug.LogError($"[EffectVocabulary] No entry for {element}.");
                return null;
            }
            return elements[element];
        }
    }
}
