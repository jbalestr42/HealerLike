using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures.Studio;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    public static class CreatureGrammarSamples
    {
        public const string Folder = "Assets/Render/Creatures/Data/GrammarPresets";
        public static readonly string[] Names = { "Plant Bud", "Plant Spear", "Plant Conductor", "Stone Ward", "Stone Spear", "Stone Pulse" };

        public static CreatureGrammarPreset Build(int index)
        {
            if (index < 0 || index >= Names.Length) throw new System.ArgumentOutOfRangeException(nameof(index));
            var preset = ScriptableObject.CreateInstance<CreatureGrammarPreset>();
            preset.name = preset.displayName = Names[index];
            preset.vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>("Assets/Render/Creatures/Data/LookVocabulary.asset");
            preset.side = index < 3 ? LookSide.Plant : LookSide.Stone;
            preset.head = new[] { HeadKind.Bud, HeadKind.Spear, HeadKind.Conductor, HeadKind.Ward, HeadKind.Spear, HeadKind.Pulse }[index];
            preset.count = new[] { CountBand.One, CountBand.Few, CountBand.Many, CountBand.One, CountBand.Few, CountBand.Many }[index];
            preset.stem = index % 3 == 0 ? StemBand.Quick : index % 3 == 1 ? StemBand.Steady : StemBand.Slow;
            preset.mass = index == 3 || index == 2 ? MassBand.Heavy : index == 1 || index == 4 ? MassBand.Sturdy : MassBand.Light;
            preset.reach = index % 3 == 0 ? ReachBand.Short : index % 3 == 1 ? ReachBand.Mid : ReachBand.Long;
            preset.accent = index == 0 ? EffectFamily.Heal : index == 2 || index == 3 ? EffectFamily.Boon : EffectFamily.Damage;
            preset.description = "Editable inputs to the existing look grammar. The vocabulary supplies all geometry; baking creates a separate manual recipe.";
            return preset;
        }

        [MenuItem("Tools/Render/Creature Grammar Samples")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Render/Creatures/Data", "GrammarPresets");
            for (int i = 0; i < Names.Length; i++)
            {
                string path = Folder + "/" + Names[i] + ".asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) continue;
                var preset = Build(i);
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssetIfDirty(preset);
            }
        }

        public static void CaptureAll()
        {
            Create();
            string folder = Path.GetFullPath("Logs/GrammarCaptures");
            Directory.CreateDirectory(folder);
            for (int i = 0; i < Names.Length; i++)
            {
                var preset = Build(i);
                CreatureRecipe recipe = null;
                try
                {
                    string[] errors = preset.Validate();
                    if (errors.Length != 0) throw new System.InvalidOperationException(Names[i] + ": " + string.Join("; ", errors));
                    recipe = preset.Compose();
                    using (var preview = new CreatureStudioPreview { Side = preset.Channels().side })
                    {
                        var texture = preview.Capture(recipe, 1.2f, 800, 700);
                        try { File.WriteAllBytes(Path.Combine(folder, Names[i] + ".png"), texture.EncodeToPNG()); }
                        finally { Object.DestroyImmediate(texture); }
                    }
                }
                finally { if (recipe) Object.DestroyImmediate(recipe); Object.DestroyImmediate(preset); }
            }
            Debug.Log("[Render Studio] Grammar samples captured to " + folder);
        }
    }
}
