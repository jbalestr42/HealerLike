using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // Every vocabulary element through the studio preview into Logs/SpellStudioCaptures, from the menu or batch mode
    public static class SpellStudioCapture
    {
        public static void CaptureAll()
        {
            string path = SpellStudioSamples.VocabularyPath;
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(path);
            if (!vocabulary)
            {
                Debug.LogError("[SpellStudioCapture] The spell vocabulary is missing: "
                    + SpellStudioSamples.VocabularyPath);
                return;
            }

            string output = Path.GetFullPath("Logs/SpellStudioCaptures");
            Directory.CreateDirectory(output);
            SpellStudioPreset preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            try
            {
                preset.vocabulary = vocabulary;
                using (SpellStudioPreview preview = new SpellStudioPreview())
                {
                    preview.Init();
                    foreach (EffectElement element in Enum.GetValues(typeof(EffectElement)))
                    {
                        preset.element = element;
                        preset.family = SpellStudioSamples.Family(element);
                        preset.stacks = 3;
                        preset.charges = 3;
                        preset.amount = 0.5f;
                        preset.tempo = EffectTempo.Once;
                        preview.Refresh();
                        StudioCaptureOutput.Write(preview.Capture(preset, preset.previewDuration * 0.4f, 960, 720),
                            Path.Combine(output, element + ".png"));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preset);
            }
        }
    }
}
