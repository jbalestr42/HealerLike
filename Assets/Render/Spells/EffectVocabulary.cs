using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    [Serializable]
    public struct EffectCell : IEquatable<EffectCell>
    {
        public EffectOperation operation;
        public EffectAspect aspect;

        public EffectCell(EffectOperation operation, EffectAspect aspect)
        {
            this.operation = operation;
            this.aspect = aspect;
        }

        public bool Equals(EffectCell other) { return operation == other.operation && aspect == other.aspect; }
        public override bool Equals(object obj) { return obj is EffectCell && Equals((EffectCell)obj); }
        public override int GetHashCode() { return ((int)operation * 397) ^ (int)aspect; }
    }

    [Serializable]
    public struct EffectCellEntry
    {
        public EffectElement once;
        public bool hasPeriodic;
        public EffectElement periodic;

        public EffectCellEntry(EffectElement once, EffectElement periodic, bool hasPeriodic)
        {
            this.once = once;
            this.periodic = periodic;
            this.hasPeriodic = hasPeriodic;
        }
    }

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
        public Dictionary<EffectMagnitude, float> magnitudeScales = new Dictionary<EffectMagnitude, float> {
            { EffectMagnitude.Light, 1f }, { EffectMagnitude.Solid, 1.15f }, { EffectMagnitude.Heavy, 1.3f }
        };
        public float MagnitudeScale(EffectMagnitude magnitude)
        {
            return magnitudeScales != null && magnitudeScales.TryGetValue(magnitude, out float scale)
                && float.IsFinite(scale) && scale > 0 ? scale : 1f;
        }

        [DictionaryDrawerSettings(KeyLabel = "Element", ValueLabel = "Entry")]
        public Dictionary<EffectElement, ElementEntry> elements = new Dictionary<EffectElement, ElementEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Operation and aspect", ValueLabel = "Once and periodic elements")]
        public Dictionary<EffectCell, EffectCellEntry> table = new Dictionary<EffectCell, EffectCellEntry>();

        public ElementEntry GetEntry(EffectElement element)
        {
            if (elements == null || !elements.ContainsKey(element))
            {
                Debug.LogError($"[EffectVocabulary] No entry for {element}.");
                return null;
            }
            return elements[element];
        }

        public bool TryGetElement(EffectOperation operation, EffectAspect aspect, out EffectElement element)
        {
            return TryGetElement(operation, aspect, EffectTempo.Once, out element);
        }

        public bool TryGetElement(EffectOperation operation, EffectAspect aspect, EffectTempo tempo, out EffectElement element)
        {
            EffectCellEntry entry;
            if (table == null || !table.TryGetValue(new EffectCell(operation, aspect), out entry))
            {
                element = default(EffectElement);
                return false;
            }
            if (tempo == EffectTempo.PerPeriod && entry.hasPeriodic)
            {
                element = entry.periodic;
                return true;
            }
            element = entry.once;
            return true;
        }

        static EffectElement LegacyElement(EffectOperation operation, EffectAspect aspect)
        {
            switch (operation)
            {
                case EffectOperation.Damage: return EffectElement.Burst;
                case EffectOperation.Heal: return EffectElement.Rise;
                case EffectOperation.Boon:
                    return aspect == EffectAspect.Defence ? EffectElement.Plates
                        : aspect == EffectAspect.Prevention ? EffectElement.Bud : EffectElement.Orbit;
                case EffectOperation.Ward: return EffectElement.Plates;
                case EffectOperation.Mana: return EffectElement.ManaUp;
                default: return aspect == EffectAspect.Offence ? EffectElement.Press : EffectElement.Crack;
            }
        }

        public static Dictionary<EffectCell, EffectElement> LegacyCells()
        {
            Dictionary<EffectCell, EffectElement> result = new Dictionary<EffectCell, EffectElement>();
            foreach (EffectOperation operation in Enum.GetValues(typeof(EffectOperation)))
            {
                foreach (EffectAspect aspect in Enum.GetValues(typeof(EffectAspect)))
                {
                    result[new EffectCell(operation, aspect)] = LegacyElement(operation, aspect);
                }
            }
            return result;
        }
    }
}
