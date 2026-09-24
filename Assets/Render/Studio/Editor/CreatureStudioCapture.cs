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
        // Real source rows on both sides, plus a private palette edit for the same plant row. The manifest keeps
        // each PNG tied to its source path; no game or renderer asset is changed by this capture.
        public static void CaptureRoster()
        {
            string output = Path.GetFullPath("Logs/CreatureRosterCaptures");
            Directory.CreateDirectory(output);
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(
                RenderGrammarLibraryWindow.AssetPaths[0]);
            CreatureRoster roster = new CreatureRoster();
            LookVocabulary copy = Object.Instantiate(vocabulary);
            LookPalette palette = Object.Instantiate(vocabulary.palette);
            copy.palette = palette;
            System.Text.StringBuilder manifest = new System.Text.StringBuilder("side\timage\tasset\tchannels\n");
            try
            {
                foreach (Entity.EntityType side in new[] { Entity.EntityType.Player, Entity.EntityType.Computer })
                {
                    roster.Reload(vocabulary, side);
                    for (int i = 0; i < roster.rows.Count; i++)
                    {
                        CreatureRosterRow row = roster.rows[i];
                        string file = side + "-" + i.ToString("00") + ".png";
                        Write(row.preview.Capture(row.recipe, 1.25f, 540, 450), Path.Combine(output, file));
                        manifest.AppendLine(side + "\t" + file + "\t" + row.path + "\t"
                            + row.ChannelLabel().Replace('\n', ' '));
                    }
                }
                roster.Reload(copy, Entity.EntityType.Player);
                if (roster.rows.Count > 0)
                {
                    CreatureRosterRow row = roster.rows[0];
                    Write(row.preview.Capture(row.recipe, 1.25f, 540, 450), Path.Combine(output, "palette-before.png"));
                    palette.plantBody = new Color(0.95f, 0.08f, 0.35f);
                    palette.plantStem = new Color(0.9f, 0.28f, 0.06f);
                    row.Rebuild(copy, Entity.EntityType.Player);
                    Write(row.preview.Capture(row.recipe, 1.25f, 540, 450), Path.Combine(output, "palette-after.png"));
                }
                File.WriteAllText(Path.Combine(output, "roster.tsv"), manifest.ToString());
                Debug.Log("[CreatureStudioCapture] Real roster captured: " + roster.rows.Count + " sources per side.");
            }
            finally
            {
                roster.Dispose();
                Object.DestroyImmediate(copy);
                Object.DestroyImmediate(palette);
            }
        }

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
