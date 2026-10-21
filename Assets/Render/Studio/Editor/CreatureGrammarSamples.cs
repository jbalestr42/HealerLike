using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // Six grammar presets across both sides, heads and bands, as starting points and as a capture set
    public static class CreatureGrammarSamples
    {
        public static readonly string Folder = "Assets/Render/Studio/Data/Presets";
        public static readonly string[] Names =
        {
            "Plant Bud", "Plant Spear", "Plant Conductor", "Stone Ward", "Stone Spear", "Stone Pulse"
        };

        static readonly string vocabularyPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";
        static readonly HeadKind[] heads =
        {
            HeadKind.Bud, HeadKind.Spear, HeadKind.Conductor, HeadKind.Ward, HeadKind.Spear, HeadKind.Pulse
        };
        static readonly CountBand[] counts =
        {
            CountBand.One, CountBand.Few, CountBand.Many, CountBand.One, CountBand.Few, CountBand.Many
        };
        static readonly MassBand[] masses =
        {
            MassBand.Light, MassBand.Sturdy, MassBand.Heavy, MassBand.Heavy, MassBand.Sturdy, MassBand.Light
        };
        static readonly EffectFamily[] accents =
        {
            EffectFamily.Heal, EffectFamily.Damage, EffectFamily.Boon, EffectFamily.Boon, EffectFamily.Damage,
            EffectFamily.Damage
        };
        // Quick, steady and slow stems, short, mid and long reach, in turn
        static readonly StemBand[] stems = { StemBand.Quick, StemBand.Steady, StemBand.Slow };
        static readonly ReachBand[] reaches = { ReachBand.Short, ReachBand.Mid, ReachBand.Long };

        // An unsaved preset the caller owns; null, with an error, outside the names
        public static CreatureGrammarPreset Build(int index)
        {
            if (index < 0 || index >= Names.Length)
            {
                Debug.LogError("[CreatureGrammarSamples] No sample at " + index);
                return null;
            }

            CreatureGrammarPreset preset = ScriptableObject.CreateInstance<CreatureGrammarPreset>();
            preset.name = Names[index];
            preset.displayName = Names[index];
            preset.vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(vocabularyPath);
            preset.side = LookSide.Stone;
            if (index < 3)
            {
                preset.side = LookSide.Plant;
            }

            preset.head = heads[index];
            preset.count = counts[index];
            preset.stem = stems[index % 3];
            preset.mass = masses[index];
            preset.reach = reaches[index % 3];
            preset.accent = accents[index];
            preset.description = "Editable inputs to the existing look grammar. The vocabulary supplies all geometry; "
                + "baking creates a separate manual recipe.";
            return preset;
        }

        // Writes the presets that are not in the folder yet
        [MenuItem("Tools/Render/Creature Grammar Samples")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Render/Studio/Data", "Presets");
            }

            for (int i = 0; i < Names.Length; i++)
            {
                string path = Folder + "/" + Names[i] + ".asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                {
                    continue;
                }

                CreatureGrammarPreset preset = Build(i);
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssetIfDirty(preset);
            }
        }

        // Each preset through the grammar and the studio preview into Logs/GrammarCaptures
        public static void CaptureAll()
        {
            Create();
            string folder = Path.GetFullPath("Logs/GrammarCaptures");
            Directory.CreateDirectory(folder);
            for (int i = 0; i < Names.Length; i++)
            {
                CreatureGrammarPreset preset = Build(i);
                string[] errors = CreatureGrammarValidator.Validate(preset);
                if (errors.Length != 0)
                {
                    Debug.LogError("[CreatureGrammarSamples] " + Names[i] + ": " + string.Join("; ", errors));
                    Object.DestroyImmediate(preset);
                    continue;
                }

                CreatureRecipe recipe = null;
                try
                {
                    recipe = preset.Compose();
                    using (CreatureStudioPreview preview = new CreatureStudioPreview())
                    {
                        preview.Init();
                        preview.side = preset.Channels().side;
                        StudioCaptureOutput.Write(preview.Capture(recipe, 1.2f, 800, 700),
                            Path.Combine(folder, Names[i] + ".png"));
                    }
                }
                finally
                {
                    Object.DestroyImmediate(recipe);
                    Object.DestroyImmediate(preset);
                }
            }
        }
    }
}
