using System;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Creatures.GrowthStoneParts;

namespace HealerLike.Render.Creatures
{
    // Explicit reset, never an import hook. Artists keep editing the saved arrays and shape parameters in Studio.
    public static class GrowthStoneVocabulary
    {
        public static readonly string AssetPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";

        [MenuItem("Tools/Render/Reset Creature Shapes to Growth and Stone")]
        public static void Author()
        {
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(AssetPath);
            if (!vocabulary)
            {
                Debug.LogError("[GrowthStoneVocabulary] Missing shared look vocabulary at " + AssetPath);
                return;
            }

            Undo.RecordObject(vocabulary, "Reset creature shape vocabulary");
            Apply(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[GrowthStoneVocabulary] Reset complete. Palette retained; saved shape fields remain editable.");
        }

        // Useful for private auditions/tests as well as the one explicit authoring action. Never changes a palette.
        public static void Apply(LookVocabulary vocabulary)
        {
            vocabulary.heads.Clear();
            foreach (HeadKind kind in Enum.GetValues(typeof(HeadKind)))
            {
                vocabulary.heads.Add(kind, GrowthStoneHeads.Build(kind));
            }

            vocabulary.accessories.Clear();
            foreach (AccessoryKind kind in Enum.GetValues(typeof(AccessoryKind)))
            {
                if (kind != AccessoryKind.None)
                {
                    vocabulary.accessories.Add(kind, GrowthStoneAccessories.Build(kind));
                }
            }

            vocabulary.bodies.Clear();
            vocabulary.bodies.Add(MassBand.Light, Body(0.85f, 0.7f, 0.72f, false));
            vocabulary.bodies.Add(MassBand.Sturdy, Body(1f, 1f, 1f, false));
            vocabulary.bodies.Add(MassBand.Heavy, Body(1.16f, 0.88f, 1f, true));
            vocabulary.stems.Clear();
            vocabulary.stems.Add(StemBand.Quick, Stem(1.8f, 0.13f, 0.5f));
            vocabulary.stems.Add(StemBand.Steady, Stem(0.8f, 0.16f, 0.34f));
            vocabulary.stems.Add(StemBand.Slow, Stem(0.5f, 0.23f, 0.22f));
            vocabulary.roots.Clear();
            vocabulary.roots.Add(ReachBand.Short, Root(1.1f));
            vocabulary.roots.Add(ReachBand.Mid, Root(1.5f));
            vocabulary.roots.Add(ReachBand.Long, Root(2.1f));
            vocabulary.bodyUnit = 0.55f;
            vocabulary.plantScale = 1.8f;
            vocabulary.stoneScale = 2.2f;
            vocabulary.maxParts = CreatureValidator.MaxParts;
            vocabulary.isReachPinned = false;
            vocabulary.rootCount = 8;
            vocabulary.rootThickness = 0.2f;
            vocabulary.rootHip = 0.28f;
            vocabulary.rootKnee = 0.22f;
            vocabulary.armCount = 2;
            vocabulary.layout = new LookVocabulary.LayoutEntry
            {
                maxBranch = 1.65f,
                minBranch = 0.7f,
                threeHeadScale = 0.76f,
                fiveHeadScale = 0.64f,
                threeHeadSpread = 43f,
                fiveHeadSpread = 31f,
                headClearance = 1.25f,
                branchThickness = 0.19f,
                limbSpread = 0.36f,
                limbWidth = 0.48f,
                limbThickness = 0.5f,
                limbSplay = 18f,
                limbAsymmetry = 0.1f,
                stoneNeck = 0.55f,
                extendAccessorySupports = true,
            };
        }

        static LookVocabulary.BodyEntry Body(float scale, float plantWidth, float stoneWidth, bool heavy)
        {
            LookPart plant = Part(
                "Body",
                ShapeProfile.Bulb(1.1f),
                Vector3.zero,
                new Vector3(plantWidth, plantWidth * 0.86f, plantWidth * 0.9f),
                PartRole.Body
            );
            LookPart stone = Part(
                "Body",
                ShapeProfile.Block(0.16f, 0.05f, 0.13f, 0.72f, 0.38f),
                Vector3.zero,
                new Vector3(stoneWidth, stoneWidth * 0.63f, stoneWidth * 0.85f),
                PartRole.Body
            );
            return new LookVocabulary.BodyEntry
            {
                scale = scale,
                headScale = 0.85f,
                stemScale = heavy ? 0.35f : 1f,
                bodyLift = heavy ? 0.52f : 0f,
                plant = heavy
                    ? new[]
                    {
                        plant,
                        Part(
                            "BaseBulb",
                            ShapeProfile.Bulb(1.1f),
                            new Vector3(0.05f, -0.5f, 0f),
                            new Vector3(1.06f, 0.64f, 0.93f),
                            PartRole.Body
                        ),
                    }
                    : new[] { plant },
                stone = heavy
                    ? new[]
                    {
                        stone,
                        Part(
                            "BaseBlock",
                            ShapeProfile.Block(0.12f, 0.08f, 0.1f, 0.6f, 0.25f),
                            new Vector3(-0.09f, -0.46f, 0f),
                            new Vector3(1.12f, 0.54f, 0.94f),
                            PartRole.Body
                        ),
                    }
                    : new[] { stone },
            };
        }

        static LookVocabulary.StemEntry Stem(float length, float thickness, float limb)
        {
            return new LookVocabulary.StemEntry
            {
                length = length,
                thickness = thickness,
                limbLength = limb,
                plantShape = ShapeProfile.Segment(0.2f, 0.4f),
                stoneLimbShape = ShapeProfile.Block(0.14f, 0.16f, 0.09f, 0.5f),
            };
        }

        static LookVocabulary.RootEntry Root(float reach)
        {
            return new LookVocabulary.RootEntry
            {
                reach = reach,
                segmentShape = ShapeProfile.Segment(0.27f, 0.7f),
                jointShape = ShapeProfile.Bulb(),
                taper = 0.6f,
                jointScale = 2.65f,
                thicknessScale = 1f,
            };
        }
    }
}
