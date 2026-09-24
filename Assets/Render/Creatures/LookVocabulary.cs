using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Where an accessory hangs on the body, every socket on the unit's right
    // Stored by value in assets: append new members, never reorder or remove
    public enum AccessorySocket
    {
        NeckOrbit,
        HipOrbit,
        Crook,
        Shoulder,
        Flank
    }

    // The parts a unit is built from, one entry per channel value, plus the proportions that size them.
    // A fragment is authored in its socket's space at unit scale and names colour roles, never a colour.
    [CreateAssetMenu(menuName = "Custom/Data/Render/LookVocabulary")]
    public class LookVocabulary : SerializedScriptableObject
    {
        [Serializable]
        public class HeadEntry
        {
            public LookPart[] plant = Array.Empty<LookPart>();
            public LookPart[] stone = Array.Empty<LookPart>();
            // The head fans its own copies (arch pods, cairn stones), parts show by their minCount
            public bool carriesCount;
        }

        [Serializable]
        public class AccessoryEntry
        {
            public AccessorySocket socket;
            public LookPart[] plant = Array.Empty<LookPart>();
            public LookPart[] stone = Array.Empty<LookPart>();
            // Where a mini head sits in the socket's space, and how small it is drawn
            public Vector3 miniHeadAt;
            public float miniHeadScale = 0.5f;
        }

        // The first part is the body itself, the others hang from its centre
        [Serializable]
        public class BodyEntry
        {
            public float scale = 1f;
            public LookPart[] plant = Array.Empty<LookPart>();
            public LookPart[] stone = Array.Empty<LookPart>();
        }

        // Lengths in body units, on stones the stem is the limb
        [Serializable]
        public class StemEntry
        {
            public float length;
            public float thickness;
            public float limbLength;
        }

        [Serializable]
        public class RootEntry
        {
            public float reach;
        }

        [DictionaryDrawerSettings(KeyLabel = "Head", ValueLabel = "Parts")]
        public Dictionary<HeadKind, HeadEntry> heads = new Dictionary<HeadKind, HeadEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Accessory", ValueLabel = "Parts")]
        public Dictionary<AccessoryKind, AccessoryEntry> accessories = new Dictionary<AccessoryKind, AccessoryEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Mass", ValueLabel = "Body")]
        public Dictionary<MassBand, BodyEntry> bodies = new Dictionary<MassBand, BodyEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Stem", ValueLabel = "Length")]
        public Dictionary<StemBand, StemEntry> stems = new Dictionary<StemBand, StemEntry>();

        [DictionaryDrawerSettings(KeyLabel = "Reach", ValueLabel = "Roots")]
        public Dictionary<ReachBand, RootEntry> roots = new Dictionary<ReachBand, RootEntry>();

        [AssetsOnly]
        public LookPalette palette;

        // Cells per body unit, the healer's body sphere
        [BoxGroup("Proportions")]
        public float bodyUnit = 0.55f;

        // A plant's body and sockets grow by this much over the body unit, so a Sturdy body reads about one cell at
        // the board camera
        [BoxGroup("Proportions")]
        public float plantScale = 1.8f;

        // A stone's body and sockets grow by this much over the body unit
        [BoxGroup("Proportions")]
        public float stoneScale = 1.6f;

        [BoxGroup("Proportions")]
        public int maxParts = CreatureValidator.MaxParts;

        // An optional art override; the roster normally uses its derived reach band.
        [BoxGroup("Proportions")]
        public bool isReachPinned;

        [BoxGroup("Proportions")]
        public float pinnedReach = 1.3f;

        [BoxGroup("Proportions")]
        public int rootCount = 10;

        // Root diameter, hip and knee heights, in the creature's own body units
        [BoxGroup("Proportions")]
        public float rootThickness = 0.14f;

        [BoxGroup("Proportions")]
        public float rootHip = 0.145f;

        [BoxGroup("Proportions")]
        public float rootKnee = 0.11f;

        [BoxGroup("Proportions")]
        public int armCount = 2;

        // Cells per body unit on a side, a plant's parts and roots are laid out at its own scale
        public float Unit(LookSide side)
        {
            return side == LookSide.Plant ? bodyUnit * plantScale : bodyUnit;
        }

        public float Reach(ReachBand band)
        {
            if (isReachPinned || !roots.ContainsKey(band))
            {
                return pinnedReach;
            }
            return roots[band].reach;
        }

        // The colour of a part for a unit's side and accent, read from the palette
        public Color Colour(ColourRole role, EffectFamily accent, LookSide side)
        {
            if (palette == null)
            {
                Debug.LogError("[LookVocabulary] No palette.");
                return Color.magenta;
            }
            return palette.Colour(role, accent, side);
        }
    }
}
