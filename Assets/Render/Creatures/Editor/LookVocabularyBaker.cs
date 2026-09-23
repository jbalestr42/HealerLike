using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Runs the code vocabulary once per entry at unit scale and writes what it drew as the vocabulary asset
    public static class LookVocabularyBaker
    {
        public static readonly string AssetPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";
        static readonly string palettePath = "Assets/Render/Grammar/Data/LookPalette.asset";
        // A colour no part uses, so the baker can tell the unit's accent from a fixed accent
        static readonly Color sentinel = new Color(1f, 0f, 1f);

        [MenuItem("Tools/Render/Bake Look Vocabulary")]
        public static void Bake()
        {
            LookPalette palette = AssetDatabase.LoadAssetAtPath<LookPalette>(palettePath);
            if (palette == null)
            {
                Debug.LogError($"[LookVocabularyBaker] Missing {palettePath}.");
                return;
            }

            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                vocabulary = ScriptableObject.CreateInstance<LookVocabulary>();
                AssetDatabase.CreateAsset(vocabulary, AssetPath);
            }

            Fill(vocabulary, palette);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LookVocabularyBaker] Wrote {AssetPath}.");
        }

        public static void Fill(LookVocabulary vocabulary, LookPalette palette)
        {
            vocabulary.palette = palette;
            vocabulary.bodyUnit = LookComposer.BodyUnit;
            vocabulary.stoneScale = LookComposer.StoneScale;
            vocabulary.accessoryReach = LookComposer.AccessoryReach;
            vocabulary.maxParts = LookComposer.MaxParts;
            vocabulary.isReachPinned = LookComposer.IsReachPinned;
            vocabulary.pinnedReach = LookComposer.PinnedReach;
            vocabulary.rootCount = LookComposer.RootCount;
            vocabulary.rootThickness = LookComposer.RootThickness;
            vocabulary.rootHip = LookComposer.RootHip;
            vocabulary.rootKnee = LookComposer.RootKnee;
            vocabulary.armCount = LookComposer.ArmCount;
            vocabulary.stoneWilt = LookComposer.StoneWilt;

            vocabulary.heads.Clear();
            foreach (HeadKind kind in Enum.GetValues(typeof(HeadKind)))
            {
                LookVocabulary.HeadEntry entry = new LookVocabulary.HeadEntry();
                entry.carriesCount = kind == HeadKind.Arch;
                entry.plant = Head(kind, LookSide.Plant, entry.carriesCount);
                entry.stone = Head(kind, LookSide.Stone, entry.carriesCount);
                vocabulary.heads[kind] = entry;
            }

            vocabulary.accessories.Clear();
            foreach (AccessoryKind kind in Enum.GetValues(typeof(AccessoryKind)))
            {
                if (kind == AccessoryKind.None)
                {
                    continue;
                }

                LookVocabulary.AccessoryEntry entry = new LookVocabulary.AccessoryEntry();
                entry.socket = PartVocabulary.Socket(kind);
                entry.plant = Accessory(kind, LookSide.Plant);
                entry.stone = Accessory(kind, LookSide.Stone);
                entry.miniHeadAt = PartVocabulary.MiniHeadAt;
                entry.miniHeadScale = PartVocabulary.MiniHeadScale;
                vocabulary.accessories[kind] = entry;
            }

            vocabulary.bodies.Clear();
            foreach (MassBand band in Enum.GetValues(typeof(MassBand)))
            {
                LookVocabulary.BodyEntry entry = new LookVocabulary.BodyEntry();
                entry.scale = LookComposer.MassScale(band);
                entry.plant = Body(band, LookSide.Plant);
                entry.stone = Body(band, LookSide.Stone);
                vocabulary.bodies[band] = entry;
            }

            vocabulary.stems.Clear();
            foreach (StemBand band in Enum.GetValues(typeof(StemBand)))
            {
                LookVocabulary.StemEntry entry = new LookVocabulary.StemEntry();
                entry.length = LookComposer.StemLength(band);
                entry.thickness = band == StemBand.Slow ? 0.28f : 0.2f;
                entry.limbLength = entry.length * 0.6f;
                vocabulary.stems[band] = entry;
            }

            vocabulary.roots.Clear();
            vocabulary.roots[ReachBand.Short] = new LookVocabulary.RootEntry { reach = LookComposer.ShortReach };
            vocabulary.roots[ReachBand.Mid] = new LookVocabulary.RootEntry { reach = LookComposer.MidReach };
            vocabulary.roots[ReachBand.Long] = new LookVocabulary.RootEntry { reach = LookComposer.LongReach };
        }

        // A head that fans its own copies is drawn at one, three and five, each part keeps the fewest copies it shows at
        static LookPart[] Head(HeadKind kind, LookSide side, bool carriesCount)
        {
            PartList five = new PartList(1f);
            PartVocabulary.Head(five, kind, side, Vector3.zero, 1f, carriesCount ? 5 : 1, sentinel);
            LookPart[] parts = Sources(five, side, PartRole.Head);
            if (!carriesCount)
            {
                return parts;
            }

            PartList one = new PartList(1f);
            PartVocabulary.Head(one, kind, side, Vector3.zero, 1f, 1, sentinel);
            PartList three = new PartList(1f);
            PartVocabulary.Head(three, kind, side, Vector3.zero, 1f, 3, sentinel);
            for (int i = 0; i < parts.Length; i++)
            {
                if (Contains(one, parts[i]))
                {
                    parts[i].minCount = CountBand.One;
                }
                else if (Contains(three, parts[i]))
                {
                    parts[i].minCount = CountBand.Few;
                }
                else
                {
                    parts[i].minCount = CountBand.Many;
                }
            }

            CheckOrder(kind, side, parts, one, CountBand.One);
            CheckOrder(kind, side, parts, three, CountBand.Few);
            return parts;
        }

        static LookPart[] Accessory(AccessoryKind kind, LookSide side)
        {
            PartList parts = new PartList(1f);
            PartVocabulary.Accessory(parts, kind, side, Vector3.zero, sentinel);
            return Sources(parts, side, PartRole.Accessory);
        }

        static LookPart[] Body(MassBand band, LookSide side)
        {
            PartList parts = new PartList(1f);
            LookComposer.Body(parts, side, band, Vector3.zero, 1f);
            return Sources(parts, side, PartRole.Body);
        }

        // The parts as the code drew them, a colour turned into its role and a role given by what the part does
        static LookPart[] Sources(PartList parts, LookSide side, PartRole fallback)
        {
            LookPart[] result = new LookPart[parts.count];
            for (int i = 0; i < parts.count; i++)
            {
                LookPart part = parts.Source(i);
                part.colour = Role(parts.Colour(i), side);
                part.minCount = CountBand.One;
                part.role = fallback;
                if (fallback == PartRole.Head)
                {
                    if (part.id == "Crown")
                    {
                        part.role = PartRole.Crown;
                    }
                    else if (part.primitive == Primitive.Capsule)
                    {
                        part.role = PartRole.Stem;
                    }
                    else if (part.colour == ColourRole.Accent)
                    {
                        part.role = PartRole.Tip;
                    }
                }
                else if (fallback == PartRole.Accessory && part.colour == ColourRole.Accent && part.id == "Bud")
                {
                    part.role = PartRole.Tip;
                }

                result[i] = part;
            }
            return result;
        }

        static ColourRole Role(Color colour, LookSide side)
        {
            if (colour == sentinel)
            {
                return ColourRole.Accent;
            }

            if (colour == PartVocabulary.PlantBody || colour == PartVocabulary.StoneBody)
            {
                return ColourRole.Body;
            }

            if (colour == PartVocabulary.PlantStem)
            {
                return ColourRole.Stem;
            }

            if (colour == PartVocabulary.StoneLimb)
            {
                return ColourRole.Limb;
            }

            if (colour == PartVocabulary.Moss)
            {
                return ColourRole.Moss;
            }

            if (colour == PartVocabulary.Accent(EffectFamily.Boon))
            {
                return ColourRole.BoonAccent;
            }

            if (colour == PartVocabulary.Accent(EffectFamily.Bane))
            {
                return ColourRole.BaneAccent;
            }

            if (colour == PartVocabulary.Accent(EffectFamily.Rot))
            {
                return ColourRole.RotAccent;
            }

            Debug.LogError($"[LookVocabularyBaker] No colour role for {colour} on {side}.");
            return ColourRole.Body;
        }

        static bool Contains(PartList parts, LookPart part)
        {
            for (int i = 0; i < parts.count; i++)
            {
                LookPart other = parts.Source(i);
                if (other.id == part.id && other.primitive == part.primitive
                    && (other.position - part.position).sqrMagnitude < 0.0000000001f
                    && (other.size - part.size).sqrMagnitude < 0.0000000001f)
                {
                    return true;
                }
            }
            return false;
        }

        // Keeping the parts a band allows must give the code's own list for that band, in its order
        static void CheckOrder(HeadKind kind, LookSide side, LookPart[] parts, PartList expected, CountBand band)
        {
            List<LookPart> kept = new List<LookPart>();
            foreach (LookPart part in parts)
            {
                if (part.minCount <= band)
                {
                    kept.Add(part);
                }
            }

            bool isSame = kept.Count == expected.count;
            for (int i = 0; isSame && i < kept.Count; i++)
            {
                LookPart other = expected.Source(i);
                isSame = kept[i].id == other.id && (kept[i].position - other.position).sqrMagnitude < 0.0000000001f;
            }

            if (!isSame)
            {
                Debug.LogError($"[LookVocabularyBaker] {kind} on {side} at {band} does not keep the code's parts in order.");
            }
        }
    }
}
