using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's starting recipes, deep copies, and the checks the runtime validator does not make
    public static class CreatureStudioAuthoring
    {
        public static readonly string[] SampleNames = { "Healer", "Sprout", "Stone Sentinel" };

        static readonly string dataFolder = "Assets/Render/Creatures/Data/";

        // The shipped healer, or a plant sprout or stone sentinel composed from the look vocabulary. The caller owns
        // it; null, with an error, outside the sample names.
        public static CreatureRecipe BuildSample(int index)
        {
            if (index < 0 || index >= SampleNames.Length)
            {
                Debug.LogError("[CreatureStudioAuthoring] No sample at " + index);
                return null;
            }

            if (index == 0)
            {
                return Clone(AssetDatabase.LoadAssetAtPath<CreatureRecipe>(dataFolder + "Healer.asset"));
            }

            string vocabularyPath = dataFolder + "LookVocabulary.asset";
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(vocabularyPath);
            if (vocabulary == null)
            {
                return null;
            }

            CreatureRecipe result = LookComposer.Compose(SampleChannels(index == 2), vocabulary);
            if (result != null)
            {
                result.name = SampleNames[index];
                result.hideFlags = HideFlags.None;
            }
            return result;
        }

        // Every array copied, the arms' rest joints too, so edits to the copy never reach the source
        public static CreatureRecipe Clone(CreatureRecipe source)
        {
            if (source == null)
            {
                return null;
            }

            CreatureRecipe result = ScriptableObject.CreateInstance<CreatureRecipe>();
            result.name = source.name;
            result.parts = Array.Empty<CreaturePart>();
            if (source.parts != null)
            {
                result.parts = (CreaturePart[])source.parts.Clone();
            }

            result.arms = Array.Empty<ArmDefinition>();
            if (source.arms != null)
            {
                result.arms = (ArmDefinition[])source.arms.Clone();
            }

            for (int i = 0; i < result.arms.Length; i++)
            {
                result.arms[i].restJoints = Array.Empty<Vector3>();
                if (source.arms[i].restJoints != null)
                {
                    result.arms[i].restJoints = (Vector3[])source.arms[i].restJoints.Clone();
                }
            }

            result.sourceLocal = Array.Empty<Vector3>();
            if (source.sourceLocal != null)
            {
                result.sourceLocal = (Vector3[])source.sourceLocal.Clone();
            }

            result.roots = source.roots;
            result.idle = source.idle;
            result.neckLocal = source.neckLocal;
            result.wiltColour = source.wiltColour;
            result.stoneOchre = source.stoneOchre;
            return result;
        }

        // The runtime validator's error, then the fields it does not read
        public static string[] Validate(CreatureRecipe recipe)
        {
            List<string> warnings = new List<string>();
            string error;
            if (!CreatureValidator.TryValidate(recipe, out error))
            {
                warnings.Add(error);
            }

            if (recipe == null)
            {
                return warnings.ToArray();
            }

            if (!RenderMath.IsFinite(recipe.neckLocal))
            {
                warnings.Add("Neck coordinates must be finite.");
            }

            if (!RenderMath.IsFinite(recipe.wiltColour) || !RenderMath.IsFinite(recipe.stoneOchre))
            {
                warnings.Add("Wilt and stone colours must be finite.");
            }

            if (recipe.parts != null && HasUnknownRole(recipe.parts))
            {
                warnings.Add("Every part needs a valid role.");
            }

            if (recipe.arms != null && HasUnknownTip(recipe.arms))
            {
                warnings.Add("Arm tip colours must be finite.");
            }
            return warnings.ToArray();
        }

        static UnitChannels SampleChannels(bool isStone)
        {
            UnitChannels channels = new UnitChannels();
            channels.count = CountBand.One;
            channels.accessory = AccessoryKind.None;
            if (isStone)
            {
                channels.side = LookSide.Stone;
                channels.head = HeadKind.Ward;
                channels.stem = StemBand.Steady;
                channels.mass = MassBand.Heavy;
                channels.reach = ReachBand.Mid;
                channels.accent = EffectFamily.Boon;
            }
            else
            {
                channels.side = LookSide.Plant;
                channels.head = HeadKind.Bud;
                channels.stem = StemBand.Quick;
                channels.mass = MassBand.Light;
                channels.reach = ReachBand.Short;
                channels.accent = EffectFamily.Heal;
            }
            return channels;
        }

        static bool HasUnknownRole(CreaturePart[] parts)
        {
            foreach (CreaturePart part in parts)
            {
                if (!Enum.IsDefined(typeof(PartRole), part.role))
                {
                    return true;
                }
            }
            return false;
        }

        static bool HasUnknownTip(ArmDefinition[] arms)
        {
            foreach (ArmDefinition arm in arms)
            {
                if (!RenderMath.IsFinite(arm.tipColour))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
