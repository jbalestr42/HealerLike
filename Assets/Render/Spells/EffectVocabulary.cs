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
        // The caster's material. Appended: every cell saved before it reads as Plant
        public LookSide side;

        public EffectCell(EffectOperation operation, EffectAspect aspect, LookSide side = LookSide.Plant)
        {
            this.operation = operation;
            this.aspect = aspect;
            this.side = side;
        }

        public bool Equals(EffectCell other)
        {
            return operation == other.operation && aspect == other.aspect && side == other.side;
        }
        public override bool Equals(object obj) { return obj is EffectCell && Equals((EffectCell)obj); }
        public override int GetHashCode() { return (((int)operation * 397) ^ (int)aspect) * 31 + (int)side; }
    }

    // A kind refines the cell it sits in: Flat on a Boon and Flat on a Bane stay apart, and a kind drawn in
    // the caster's material falls back to its Plant entry like a cell does
    [Serializable]
    public struct EffectKindCell : IEquatable<EffectKindCell>
    {
        public EffectOperation operation;
        public EffectAspect aspect;
        public EffectKind kind;
        public LookSide side;

        public EffectKindCell(EffectOperation operation, EffectAspect aspect, EffectKind kind,
                              LookSide side = LookSide.Plant)
        {
            this.operation = operation;
            this.aspect = aspect;
            this.kind = kind;
            this.side = side;
        }

        public bool Equals(EffectKindCell other)
        {
            return operation == other.operation && aspect == other.aspect && kind == other.kind && side == other.side;
        }
        public override bool Equals(object obj) { return obj is EffectKindCell && Equals((EffectKindCell)obj); }
        public override int GetHashCode()
        {
            return ((((int)operation * 397) ^ (int)aspect) * 31 + (int)kind) * 31 + (int)side;
        }
    }

    [Serializable]
    public struct EffectCellEntry
    {
        public EffectKey once;
        public bool hasPeriodic;
        public EffectKey periodic;

        public EffectCellEntry(EffectKey once, EffectKey periodic, bool hasPeriodic)
        {
            this.once = once;
            this.periodic = periodic;
            this.hasPeriodic = hasPeriodic;
        }
    }

    [Serializable]
    public struct EffectCellEntries
    {
        public ElementEntry once;
        public ElementEntry periodic;
        public bool hasPeriodic;

        public EffectCellEntries(ElementEntry once, ElementEntry periodic, bool hasPeriodic)
        {
            this.once = once;
            this.periodic = periodic;
            this.hasPeriodic = hasPeriodic;
        }
    }

    // Additive authored compositions have no operation/aspect cell, so they use a
    // deliberately small vocabulary of stable piece keys.
    public enum EffectPiece
    {
        Beam,
        Ring,
        Litter
    }

    // What an effect draws, the composer picks one from the family and the group of a handler
    // Stored by value in assets: append new members, never reorder or remove
    public enum EffectKey
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
        Litter,
        Dart,
        Seeds,
        Cadence,
        Brackets,
        Footring,
        Canopy,
        Spark,
        Echo,
        Tether,
        Sprout,
        Stem
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
        public Dictionary<EffectKey, ElementEntry> entries = new Dictionary<EffectKey, ElementEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Operation and aspect", ValueLabel = "Once and periodic elements")]
        public Dictionary<EffectCell, EffectCellEntry> legacyTable = new Dictionary<EffectCell, EffectCellEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Operation and aspect", ValueLabel = "Once and periodic entries")]
        public Dictionary<EffectCell, EffectCellEntries> cells = new Dictionary<EffectCell, EffectCellEntries>();

        // Consulted before the cells: a handler whose kind has an entry here draws it instead of its cell's
        [DictionaryDrawerSettings(KeyLabel = "Operation, aspect, kind and material", ValueLabel = "Entry")]
        public Dictionary<EffectKindCell, ElementEntry> kinds = new Dictionary<EffectKindCell, ElementEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Piece", ValueLabel = "Entry")]
        public Dictionary<EffectPiece, ElementEntry> pieces = new Dictionary<EffectPiece, ElementEntry>();

        // Optional additive pieces. Missing keys leave the core unchanged; each piece owns its socket.
        [DictionaryDrawerSettings(KeyLabel = "Reach", ValueLabel = "Piece")]
        public Dictionary<EffectReach, ElementEntry> reach = new Dictionary<EffectReach, ElementEntry>();
        [DictionaryDrawerSettings(KeyLabel = "Delivery", ValueLabel = "Piece")]
        public Dictionary<EffectDelivery, ElementEntry> delivery = new Dictionary<EffectDelivery, ElementEntry>();
        [DictionaryDrawerSettings(KeyLabel = "Trigger", ValueLabel = "Piece")]
        public Dictionary<EffectTrigger, ElementEntry> trigger = new Dictionary<EffectTrigger, ElementEntry>();
        [DictionaryDrawerSettings(KeyLabel = "Side", ValueLabel = "Piece")]
        public Dictionary<EffectSide, ElementEntry> side = new Dictionary<EffectSide, ElementEntry>();
        [DictionaryDrawerSettings(KeyLabel = "Origin", ValueLabel = "Piece")]
        public Dictionary<EffectOrigin, ElementEntry> origin = new Dictionary<EffectOrigin, ElementEntry>();

        public ElementEntry GetEntry(EffectKey element)
        {
            if (entries != null && entries.TryGetValue(element, out ElementEntry direct) && direct != null)
            {
                return direct;
            }
            if (pieces != null && element >= EffectKey.Beam && pieces.TryGetValue((EffectPiece)(element - EffectKey.Beam), out ElementEntry piece)
                && piece != null)
            {
                return piece;
            }
            if (TryGetKindEntry(element, LookSide.Plant, out ElementEntry kindEntry)) return kindEntry;
            foreach (var pair in cells ?? new Dictionary<EffectCell, EffectCellEntries>())
            {
                if (pair.Key.side != LookSide.Plant) continue;
                if (KeyFor(pair.Key, EffectTempo.Once) == element && pair.Value.once != null) return pair.Value.once;
                if (KeyFor(pair.Key, EffectTempo.PerPeriod) == element && pair.Value.periodic != null) return pair.Value.periodic;
            }
            Debug.LogError($"[EffectVocabulary] No entry for {element}.");
            return null;
        }

        // The element drawn in the caster's material. A material with no entry of its own for this element draws
        // the Plant entry, and says so once per element through the log and through drawn.
        public ElementEntry GetEntry(EffectKey element, LookSide material, out LookSide drawn)
        {
            drawn = LookSide.Plant;
            if (material != LookSide.Plant && TryGetKindEntry(element, material, out ElementEntry kindEntry))
            {
                drawn = material;
                return kindEntry;
            }
            if (material != LookSide.Plant && KindOf(element) != EffectKind.Plain)
            {
                if (_reportedFallbacks.Add((element, material)))
                    Debug.Log($"[EffectVocabulary] No {material} entry for {element} yet, drawing its Plant entry.");
                return GetEntry(element);
            }
            if (material != LookSide.Plant && cells != null)
            {
                foreach (var pair in cells)
                {
                    if (pair.Key.side != material) continue;
                    if (KeyFor(pair.Key, EffectTempo.Once) == element && pair.Value.once != null)
                    {
                        drawn = material;
                        return pair.Value.once;
                    }
                    if (pair.Value.hasPeriodic && KeyFor(pair.Key, EffectTempo.PerPeriod) == element
                        && pair.Value.periodic != null)
                    {
                        drawn = material;
                        return pair.Value.periodic;
                    }
                }
                if (_reportedFallbacks.Add((element, material)))
                    Debug.Log($"[EffectVocabulary] No {material} entry for {element} yet, drawing its Plant entry.");
            }
            return GetEntry(element);
        }

        [NonSerialized]
        readonly HashSet<(EffectKey, LookSide)> _reportedFallbacks = new HashSet<(EffectKey, LookSide)>();

        // The element of a kind that has an entry for this cell in the caster's material or in Plant.
        // Plain and a kind with no entry at all return false: the caller falls through to the cells
        public bool TryGetKindElement(EffectOperation operation, EffectAspect aspect, EffectKind kind,
                                      LookSide material, out EffectKey element)
        {
            element = KindKey(kind);
            if (kind == EffectKind.Plain || kinds == null) return false;
            return Has(new EffectKindCell(operation, aspect, kind, material))
                || Has(new EffectKindCell(operation, aspect, kind, LookSide.Plant));
        }

        bool Has(EffectKindCell cell) { return kinds.TryGetValue(cell, out ElementEntry entry) && entry != null; }

        bool TryGetKindEntry(EffectKey element, LookSide side, out ElementEntry entry)
        {
            entry = null;
            EffectKind kind = KindOf(element);
            if (kind == EffectKind.Plain || kinds == null) return false;
            foreach (var pair in kinds)
            {
                if (pair.Key.kind != kind || pair.Key.side != side || pair.Value == null) continue;
                entry = pair.Value;
                return true;
            }
            return false;
        }

        public static EffectKey KindKey(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Projectile: return EffectKey.Dart;
                case EffectKind.Volume: return EffectKey.Seeds;
                case EffectKind.Rate: return EffectKey.Cadence;
                case EffectKind.Conditional: return EffectKey.Brackets;
                case EffectKind.Positional: return EffectKey.Footring;
                case EffectKind.Flat: return EffectKey.Canopy;
                case EffectKind.Reactive: return EffectKey.Spark;
                case EffectKind.Echo: return EffectKey.Echo;
                case EffectKind.Link: return EffectKey.Tether;
                case EffectKind.Summon: return EffectKey.Sprout;
                case EffectKind.Growth: return EffectKey.Stem;
                default: return default(EffectKey);
            }
        }

        public static EffectKind KindOf(EffectKey element)
        {
            switch (element)
            {
                case EffectKey.Dart: return EffectKind.Projectile;
                case EffectKey.Seeds: return EffectKind.Volume;
                case EffectKey.Cadence: return EffectKind.Rate;
                case EffectKey.Brackets: return EffectKind.Conditional;
                case EffectKey.Footring: return EffectKind.Positional;
                case EffectKey.Canopy: return EffectKind.Flat;
                case EffectKey.Spark: return EffectKind.Reactive;
                case EffectKey.Echo: return EffectKind.Echo;
                case EffectKey.Tether: return EffectKind.Link;
                case EffectKey.Sprout: return EffectKind.Summon;
                case EffectKey.Stem: return EffectKind.Growth;
                default: return EffectKind.Plain;
            }
        }

        public bool TryGetElement(EffectOperation operation, EffectAspect aspect, out EffectKey element)
        {
            return TryGetElement(operation, aspect, EffectTempo.Once, out element);
        }

        public bool TryGetElement(EffectOperation operation, EffectAspect aspect, EffectTempo tempo, out EffectKey element)
        {
            EffectCell cell = new EffectCell(operation, aspect);
            if (legacyTable != null && legacyTable.TryGetValue(cell, out EffectCellEntry legacy))
            {
                element = tempo == EffectTempo.PerPeriod && legacy.hasPeriodic ? legacy.periodic : legacy.once;
                return true;
            }
            element = KeyFor(cell, tempo);
            return cells != null && cells.ContainsKey(cell);
        }

        static EffectKey KeyFor(EffectCell cell, EffectTempo tempo)
        {
            if (tempo == EffectTempo.PerPeriod)
            {
                if (cell.operation == EffectOperation.Damage) return EffectKey.Drips;
                if (cell.operation == EffectOperation.Heal) return EffectKey.Stalks;
            }
            switch (cell.operation)
            {
                case EffectOperation.Damage: return EffectKey.Burst;
                case EffectOperation.Heal: return EffectKey.Rise;
                case EffectOperation.Boon: return cell.aspect == EffectAspect.Defence ? EffectKey.Plates
                    : cell.aspect == EffectAspect.Prevention ? EffectKey.Bud : EffectKey.Orbit;
                case EffectOperation.Ward: return cell.aspect == EffectAspect.Prevention ? EffectKey.Bud : EffectKey.Plates;
                case EffectOperation.Mana: return EffectKey.ManaUp;
                case EffectOperation.ManaDrain: return EffectKey.ManaDown;
                default: return cell.aspect == EffectAspect.Offence ? EffectKey.Press : EffectKey.Crack;
            }
        }
    }
}
