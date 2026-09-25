using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Where an accessory hangs on the body
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
            // Zero keeps the original cadence length; positive values scale only this family's plant stem.
            public float plantStemScale;
            // The head fans its own copies (arch pods, cairn stones), parts show by their minCount
            public bool carriesCount;
        }

        [Serializable]
        public class AccessoryEntry
        {
            public AccessorySocket socket;
            // Collars/crowns can surround their socket; older accessories keep the right-side clearance rule.
            public bool isCentered;
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
            // Zero preserves old assets: head scale inherits scale; cadence length is 1 on plants, scale on stones.
            public float headScale;
            public float stemScale;
            public float HeadScale => headScale > 0f ? headScale : scale;
            public float StemScale(bool isPlant) => stemScale > 0f ? stemScale : (isPlant ? 1f : scale);
            // Lift a compound base enough to expose its additional basal mass.
            public float bodyLift;
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
            public ShapeProfile plantShape;
            public ShapeProfile stoneLimbShape;
        }

        [Serializable]
        public class RootEntry
        {
            public float reach;
            public float thicknessScale = 1f;
            public float taper = 0.65f;
            public float jointScale = 2.8f;
            public ShapeProfile segmentShape;
            public ShapeProfile jointShape;
            // Odin assets predating profiles can omit this field; zero is their original unscaled thickness.
            public float ThicknessScale => thicknessScale == 0f && !segmentShape.isProcedural
                && !jointShape.isProcedural ? 1f : thicknessScale;
        }

        // Shared construction proportions. Band-specific sizes and profiles stay in their entries above.
        [Serializable]
        public class LayoutEntry
        {
            public float plantBodySink = 0.84f;
            public float plantStemFoot = 0.8f;
            public float stoneBodyLift = 0.8f;
            public float stoneNeck = 0.75f;
            public float shoulderOffset = 0.7f;
            public float limbSpread = 0.36f;
            public float limbDepth = 0.05f;
            public float limbWidth = 0.42f;
            public float limbThickness = 0.45f;
            public float limbSplay = 25f;
            public float limbBodyOverlap = 0.5f;
            public float minBranch = 0.5f;
            public float maxBranch = 1.2f;
            public float threeHeadScale = 0.72f;
            public float fiveHeadScale = 0.55f;
            public float threeHeadSpread = 40f;
            public float fiveHeadSpread = 28f;
            public float stoneBranch = 0.35f;
            public float stoneBranchThickness = 0.24f;
            public float branchThickness = 0.12f;
            public float headClearance = 1.2f;
            public float foreshortening = 0.85f;
            public bool extendAccessorySupports;
            // Required distance beyond the body/head outline, in board cells.
            public float plantAccessoryClearance = 0.25f;
            public float stoneAccessoryClearance = 0.3f;

            public bool IsValid()
            {
                return Positive(plantBodySink) && Positive(plantStemFoot) && Positive(stoneBodyLift)
                    && Positive(stoneNeck) && Positive(shoulderOffset) && Positive(limbSpread)
                    && float.IsFinite(limbDepth) && Positive(limbWidth) && Positive(limbThickness)
                    && float.IsFinite(limbSplay) && Mathf.Abs(limbSplay) <= 90f
                    && Positive(limbBodyOverlap) && Positive(minBranch) && maxBranch >= minBranch
                    && Positive(maxBranch) && Positive(threeHeadScale) && Positive(fiveHeadScale)
                    && Angle(threeHeadSpread) && Angle(fiveHeadSpread) && fiveHeadSpread < 45f && Positive(stoneBranch)
                    && Positive(stoneBranchThickness)
                    && Positive(branchThickness) && Positive(headClearance) && Positive(foreshortening) && foreshortening <= 1f
                    && Positive(plantAccessoryClearance) && Positive(stoneAccessoryClearance);
            }

            static bool Angle(float value) { return Positive(value) && value < 90f; }
            static bool Positive(float value) { return float.IsFinite(value) && value > 0f; }
        }

        public LayoutEntry layout = new LayoutEntry();
        // Older Odin assets can omit the new reference entirely.
        public LayoutEntry Layout { get { return layout ?? (layout = new LayoutEntry()); } }

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
