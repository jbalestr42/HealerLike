using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // Each starter recipe alone and under a held boon into Logs/CreatureStudioCaptures, from the menu or batch mode
    public static class CreatureStudioCapture
    {
        public static void CaptureAll()
        {
            CreatureStudioSamples.Create();
            string output = Path.GetFullPath("Logs/CreatureStudioCaptures");
            Directory.CreateDirectory(output);
            string path = SpellStudioSamples.VocabularyPath;
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(path);
            for (int i = 0; i < CreatureStudioAuthoring.SampleNames.Length; i++)
            {
                CreatureRecipe recipe = CreatureStudioAuthoring.BuildSample(i);
                if (recipe == null)
                {
                    continue;
                }

                LookSide side = LookSide.Plant;
                if (i == 2)
                {
                    side = LookSide.Stone;
                }

                CreatureStudioPreview creature = new CreatureStudioPreview();
                creature.Init();
                creature.side = side;
                Write(creature.Capture(recipe, 1.25f, 1000, 800), Path.Combine(output, recipe.name + ".png"));
                creature.Dispose();

                SpellStudioPreset spell = ScriptableObject.CreateInstance<SpellStudioPreset>();
                spell.vocabulary = vocabulary;
                spell.element = EffectElement.Orbit;
                spell.family = EffectFamily.Boon;
                spell.tempo = EffectTempo.ForDuration;
                SpellStudioPreview preview = new SpellStudioPreview();
                preview.Init();
                preview.target.recipe = recipe;
                preview.target.side = side;
                Write(preview.Capture(spell, 1.25f, 1000, 800), Path.Combine(output, recipe.name + " + Spell.png"));
                preview.Dispose();
                Object.DestroyImmediate(spell);
                Object.DestroyImmediate(recipe);
            }
        }

        static void Write(Texture2D image, string path)
        {
            if (image == null)
            {
                return;
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
    }
}
