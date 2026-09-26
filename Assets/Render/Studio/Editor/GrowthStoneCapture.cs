using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{
    // Native production rigs, distinct from the generated concept and the previous roster baseline.
    public static class GrowthStoneCapture
    {
        [MenuItem("Tools/Render/Capture Growth and Stone Grammar")]
        public static void CaptureAll()
        {
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(GrowthStoneVocabulary.AssetPath);
            string output = Path.GetFullPath("Logs/GrowthStoneCaptures");
            Directory.CreateDirectory(output);
            StringBuilder manifest = new StringBuilder("group\tside\timage\tlabel\tsource\tchannels\tparts\n");
            foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
            {
                CaptureRoster(vocabulary, side, output, manifest);
                CaptureGrammar(vocabulary, side, output, manifest);
            }
            File.WriteAllText(Path.Combine(output, "manifest.tsv"), manifest.ToString());
            File.WriteAllText(Path.Combine(output, "provenance.txt"),
                "Native Creature Studio capture of the saved production LookVocabulary.\n"
                + "Time: " + DateTime.UtcNow.ToString("O") + "\n"
                + "Sample time: 0; fixed camera per category and side; production palette/materials.\n"
                + "No gameplay asset is edited. These previews do not establish in-game occlusion or motion.\n");
            Debug.Log("[GrowthStoneCapture] Roster and full grammar: " + output);
        }

        static void CaptureRoster(LookVocabulary vocabulary, LookSide side, string output, StringBuilder manifest)
        {
            using (CreatureRoster roster = new CreatureRoster())
            {
                Entity.EntityType entitySide = side == LookSide.Plant
                    ? Entity.EntityType.Player : Entity.EntityType.Computer;
                roster.Reload(vocabulary, entitySide);
                for (int i = 0; i < roster.rows.Count; i++)
                {
                    CreatureRosterRow row = roster.rows[i];
                    string name = "roster-" + side + "-" + i.ToString("00") + ".png";
                    Save(row.preview.Capture(row.recipe, 0f, 720, 720), Path.Combine(output, name));
                    manifest.AppendLine("roster\t" + side + "\t" + name + "\t" + row.source.name + "\t" + row.path
                        + "\t" + row.ChannelLabel().Replace('\n', ' ') + "\t" + row.recipe.parts.Length);
                }
            }
        }

        static void CaptureGrammar(LookVocabulary vocabulary, LookSide side, string output, StringBuilder manifest)
        {
            List<Example> examples = Examples(side);
            Dictionary<string, Bounds> frames = new Dictionary<string, Bounds>();
            CreatureStudioPreview preview = new CreatureStudioPreview();
            preview.Init();
            preview.side = side;
            try
            {
                foreach (Example example in examples)
                {
                    example.recipe = LookComposer.Compose(example.channels, vocabulary);
                    if (!example.recipe || preview.Sample(example.recipe, 0f) == null)
                    {
                        throw new InvalidOperationException("Cannot preview " + side + " " + example.name);
                    }
                    Bounds bounds = preview.GetContentBounds();
                    if (frames.TryGetValue(example.group, out Bounds previous))
                    {
                        bounds.Encapsulate(previous);
                    }
                    frames[example.group] = bounds;
                }
                foreach (Example example in examples)
                {
                    preview.framingBounds = frames[example.group];
                    string name = example.group + "-" + side + "-" + example.name + ".png";
                    Save(preview.Capture(example.recipe, 0f, 720, 720), Path.Combine(output, name));
                    manifest.AppendLine(example.group + "\t" + side + "\t" + name + "\t" + example.name
                        + "\tmanual grammar\t" + JsonUtility.ToJson(example.channels)
                        + "\t" + example.recipe.parts.Length);
                }
            }
            finally
            {
                preview.Dispose();
                foreach (Example example in examples)
                {
                    if (example.recipe)
                    {
                        Object.DestroyImmediate(example.recipe);
                    }
                }
            }
        }

        static List<Example> Examples(LookSide side)
        {
            List<Example> examples = new List<Example>();
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                UnitChannels channels = Baseline(side);
                channels.head = head;
                examples.Add(new Example("heads", head.ToString(), channels));
            }
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                UnitChannels channels = Baseline(side);
                channels.accessory = accessory;
                examples.Add(new Example("accessories", accessory.ToString(), channels));
            }
            foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
            {
                UnitChannels channels = Baseline(side);
                channels.count = count;
                examples.Add(new Example("count", count.ToString(), channels));
                channels.head = HeadKind.Arch;
                examples.Add(new Example("arch-count", count.ToString(), channels));
            }
            foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
            {
                UnitChannels channels = Baseline(side);
                channels.stem = stem;
                examples.Add(new Example("stem", stem.ToString(), channels));
            }
            foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
            {
                UnitChannels channels = Baseline(side);
                channels.mass = mass;
                examples.Add(new Example("mass", mass.ToString(), channels));
            }
            foreach (ReachBand reach in Enum.GetValues(typeof(ReachBand)))
            {
                UnitChannels channels = Baseline(side);
                channels.reach = reach;
                examples.Add(new Example("reach", reach.ToString(), channels));
            }
            foreach (EffectFamily accent in Enum.GetValues(typeof(EffectFamily)))
            {
                UnitChannels channels = Baseline(side);
                channels.accent = accent;
                examples.Add(new Example("accents", accent.ToString(), channels));
            }
            return examples;
        }

        public static UnitChannels Baseline(LookSide side)
        {
            return new UnitChannels
            {
                side = side, head = HeadKind.Bud, count = CountBand.One, stem = StemBand.Steady,
                mass = MassBand.Light, reach = ReachBand.Short, accessory = AccessoryKind.None,
                accessoryHead = HeadKind.Bud, accent = EffectFamily.Damage
            };
        }

        static void Save(Texture2D image, string path)
        {
            if (!image)
            {
                throw new InvalidOperationException("Capture returned no image: " + path);
            }
            StudioCaptureOutput.Write(image, path);
        }

        class Example
        {
            public readonly string group;
            public readonly string name;
            public readonly UnitChannels channels;
            public CreatureRecipe recipe;

            public Example(string group, string name, UnitChannels channels)
            {
                this.group = group;
                this.name = name;
                this.channels = channels;
            }
        }
    }
}
