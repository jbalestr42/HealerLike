using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Spells.Studio;
using HealerLike.Render.Spells.Editor.Studio;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    public static class CreatureStudioCapture
    {
        public static void CaptureAll()
        {
            CreatureStudioSamples.Create();
            string output = Path.GetFullPath("Logs/CreatureStudioCaptures");
            Directory.CreateDirectory(output);
            for (int i = 0; i < CreatureStudioAuthoring.SampleNames.Length; i++)
            {
                var recipe = CreatureStudioAuthoring.BuildSample(i);
                try
                {
                    using (var preview = new CreatureStudioPreview { Side = i == 2 ? LookSide.Stone : LookSide.Plant })
                        Write(preview.Capture(recipe, 1.25f, 1000, 800), Path.Combine(output, recipe.name + ".png"));

                    var spell = ScriptableObject.CreateInstance<SpellStudioPreset>();
                    try
                    {
                        spell.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
                        spell.element = EffectElement.Orbit;
                        spell.family = EffectFamily.Boon;
                        spell.tempo = EffectTempo.ForDuration;
                        using (var preview = new SpellStudioPreview { ReferenceRecipe = recipe, TargetSide = i == 2 ? LookSide.Stone : LookSide.Plant })
                            Write(preview.Capture(spell, 1.25f, 1000, 800), Path.Combine(output, recipe.name + " + Spell.png"));
                    }
                    finally { Object.DestroyImmediate(spell); }
                }
                finally { if (recipe) Object.DestroyImmediate(recipe); }
            }
            Debug.Log("[Creature Studio] Captured creature and spell combinations to " + output);
        }

        static void Write(Texture2D image, string path)
        {
            try { File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { Object.DestroyImmediate(image); }
        }
    }
}
