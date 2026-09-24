using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Studio;

namespace HealerLike.Render.Spells.Editor.Studio
{
    /// <summary>Repeatable visual smoke artifacts, also callable from Unity batch mode.</summary>
    public static class SpellStudioCapture
    {
        public static void CaptureAll()
        {
            string output = Path.GetFullPath("Logs/SpellStudioCaptures");
            Directory.CreateDirectory(output);
            var vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            if (!vocabulary) throw new InvalidOperationException("The spell vocabulary is missing.");
            var preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            preset.vocabulary = vocabulary;
            try
            {
                using (var preview = new SpellStudioPreview())
                    foreach (EffectElement element in Enum.GetValues(typeof(EffectElement)))
                    {
                        preset.element = element;
                        preset.family = Family(element);
                        preset.stacks = 3;
                        preset.charges = 3;
                        preset.amount = .5f;
                        preset.tempo = EffectTempo.Once;
                        preview.Refresh();
                        var texture = preview.Capture(preset, preset.PreviewDuration * .4f, 960, 720);
                        try { File.WriteAllBytes(Path.Combine(output, element + ".png"), texture.EncodeToPNG()); }
                        finally { UnityEngine.Object.DestroyImmediate(texture); }
                    }
            }
            finally { UnityEngine.Object.DestroyImmediate(preset); }
            Debug.Log("[Spell Studio] Captured all 14 vocabulary elements to " + output);
        }

        static EffectFamily Family(EffectElement element)
        {
            switch (element)
            {
                case EffectElement.Rise: return EffectFamily.Heal;
                case EffectElement.Stalks: return EffectFamily.Renew;
                case EffectElement.Drips: return EffectFamily.Rot;
                case EffectElement.Orbit:
                case EffectElement.Plates:
                case EffectElement.Bud: return EffectFamily.Boon;
                case EffectElement.Press:
                case EffectElement.Crack: return EffectFamily.Bane;
                default: return EffectFamily.Damage;
            }
        }
    }
}
