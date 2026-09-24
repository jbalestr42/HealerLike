using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // What an effect draws, the composer picks one from the family and the group of a handler
    // Stored by value in assets: append new members, never reorder or remove
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
    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectMotionKind
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
    // Stored by value in assets: append new members, never reorder or remove
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
    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectCount
    {
        Fixed,
        Stacks,
        Charges,
        Amount
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
