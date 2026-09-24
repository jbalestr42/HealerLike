using System;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Studio;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells.Editor.Studio
{
    /// <summary>Editable starting points, each with its own copy of the production geometry.</summary>
    public static class SpellStudioSamples
    {
        public const string Folder = "Assets/Render/Spells/Data/StudioSamples";
        public const string VocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";
        public const int Count = 6;
        static readonly string[] Names = { "Verdant Bloom", "Rotfall", "Aegis", "Arc Link", "Astral Refill", "Ember Corona" };

        public static string NameAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Names[index];
        }

        [MenuItem("Tools/Render/Spell Studio Samples")]
        public static void Create()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(VocabularyPath);
            if (vocabulary == null)
            {
                Debug.LogError("[SpellStudioSamples] The renderer vocabulary is missing: " + VocabularyPath);
                return;
            }
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Render/Spells/Data", "StudioSamples");
            int created = 0;
            for (int i = 0; i < Count; i++)
            {
                string path = Folder + "/" + NameAt(i) + ".asset";
                // Existing examples are user-authored work; running this command again must preserve them.
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) continue;
                SpellStudioPreset preset = Build(vocabulary, i);
                if (preset == null) continue;
                try
                {
                    AssetDatabase.CreateAsset(preset, path);
                    AssetDatabase.SaveAssetIfDirty(preset);
                    created++;
                }
                finally
                {
                    if (preset != null && !EditorUtility.IsPersistent(preset)) UnityEngine.Object.DestroyImmediate(preset);
                }
            }
            Debug.Log("[SpellStudioSamples] Created " + created + " sample presets in " + Folder + ". Existing presets were preserved.");
        }

        /// <summary>Creates an unsaved preset. Caller owns it. Missing vocabulary entries return null.</summary>
        public static SpellStudioPreset Build(EffectVocabulary vocabulary, int index)
        {
            string title = NameAt(index);
            if (vocabulary == null) return null;
            SpellStudioPreset preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            preset.name = title;
            preset.displayName = title;
            preset.vocabulary = vocabulary;
            preset.overrideColour = true;
            preset.side = Entity.EntityType.Player;
            switch (index)
            {
                case 0:
                    preset.element = EffectElement.Rise;
                    preset.family = EffectFamily.Heal;
                    preset.tempo = EffectTempo.Once;
                    preset.amount = 0.5f;
                    preset.critical = true;
                    preset.colour = new Color(0.54f, 1f, 0.26f);
                    preset.description = "A full-strength critical heal. Lime spheres rise from the feet; scrub the first cycle to inspect the launch and critical ring. Amount controls the visible shape count.";
                    break;
                case 1:
                    preset.element = EffectElement.Drips;
                    preset.family = EffectFamily.Rot;
                    preset.tempo = EffectTempo.PerPeriod;
                    preset.periodSeconds = 1.2f;
                    preset.durationSeconds = 6f;
                    preset.stacks = 3;
                    preset.side = Entity.EntityType.Computer;
                    preset.colour = new Color(0.62f, 0.3f, 0.8f);
                    preset.description = "Three stacks of violet rot, ticking every 1.2 seconds for five cycles. The first tick starts after one period. Watch the drops swell, fall and reset.";
                    break;
                case 2:
                    preset.element = EffectElement.Plates;
                    preset.family = EffectFamily.Boon;
                    preset.tempo = EffectTempo.ForDuration;
                    preset.durationSeconds = 5f;
                    preset.charges = 3f;
                    preset.colour = new Color(1f, 0.77f, 0.27f);
                    preset.description = "A warm gold, three-charge ward. Plates close around the body and remain held. Lower Charges to study how the vocabulary reveals each plate.";
                    break;
                case 3:
                    preset.element = EffectElement.Beam;
                    preset.family = EffectFamily.Damage;
                    preset.tempo = EffectTempo.ForDuration;
                    preset.durationSeconds = 4f;
                    preset.colour = new Color(0.35f, 0.77f, 1f);
                    preset.description = "A cool blue connection with travelling beads. The link socket spans the preview endpoints; orbit the camera to inspect the curve. Shape edits remain local to this preset.";
                    break;
                case 4:
                    preset.element = EffectElement.ManaUp;
                    preset.family = EffectFamily.Heal;
                    preset.tempo = EffectTempo.Once;
                    preset.amount = 0.4f;
                    preset.colour = new Color(0.42f, 0.66f, 1f);
                    preset.description = "A sky-blue mana gain. An isolated impact example for comparing the mana silhouette with Verdant Bloom; loop the preview to inspect the upward motion.";
                    break;
                default:
                    preset.element = EffectElement.Burst;
                    preset.family = EffectFamily.Damage;
                    preset.tempo = EffectTempo.Once;
                    preset.amount = 0.5f;
                    preset.critical = true;
                    preset.side = Entity.EntityType.Computer;
                    preset.scale = 1.2f;
                    preset.colour = new Color(1f, 0.36f, 0.12f);
                    preset.description = "An orange critical impact with a slightly enlarged silhouette. Scrub near the opening frames to inspect the burst expansion and compare it with the unscaled vocabulary.";
                    break;
            }
            if (preset.CaptureEntry()) return preset;
            UnityEngine.Object.DestroyImmediate(preset);
            return null;
        }
    }
}
