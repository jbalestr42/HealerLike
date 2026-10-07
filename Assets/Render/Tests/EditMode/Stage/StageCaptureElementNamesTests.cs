using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Every element a capture names by a string literal in Q(, PointerTap( or Submit( still exists in the HUD.
    //
    // Four capture modes died on 2026-09-26 when party-button left GameUI, and nothing failed until somebody ran
    // them ten days later. This reads the capture sources as text and looks each literal name up in the cloned
    // UI/Toolkit/GameUI tree, plus the two item templates the HUD stamps into it at runtime (DataCard for every
    // roster and spell card, Controls/MapNode for every map node), so it runs in the suite with no play session.
    //
    // Its limit: it catches a name that no longer exists, not a name whose meaning changed. It would not have
    // caught card-status going permanently empty, or a tap on a roster card turning from deploy into inspect, and
    // those two cost the most to find. It also skips names built at runtime ("map-node-" + floor) or held in a
    // variable or a constant; only a whole string literal argument is read.
    public class StageCaptureElementNamesTests
    {
        const string CaptureFolder = "Render/Stage/Editor/Capture";

        static readonly string[] Templates = { "UI/Toolkit/GameUI", "UI/Toolkit/DataCard", "UI/Toolkit/Controls/MapNode" };

        // Created by our own render code rather than authored in a template, with the type that creates it
        static readonly Dictionary<string, string> RuntimeNames = new Dictionary<string, string>
        {
            { "render-card-art", "StageIconLabels" },
        };

        static readonly Regex Call = new Regex(@"(?:\bQ(?:<\w+>)?|\bPointerTap|\bSubmit)\(\s*""([^""]+)""\s*\)");

        public static IEnumerable<string> Names(string source)
        {
            return Call.Matches(source).Cast<Match>().Select(match => match.Groups[1].Value);
        }

        [Test]
        public void Names_EachCallShape_ReadsTheWholeLiteralOnly()
        {
            string source = "root.Q(\"a\"); root.Q<Button>(\"b\"); actions.PointerTap(\"c\"); actions.Submit( \"d\" );"
                + " root.Q(\"map-node-\" + floor); root.Q(name); root.Query<Button>(\"e\"); Equip(\"f\");";

            Assert.That(Names(source), Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void CaptureSources_EveryLiteralElementName_ExistsInTheGameUiTree()
        {
            HashSet<string> present = new HashSet<string>(RuntimeNames.Keys);
            foreach (string path in Templates)
            {
                VisualTreeAsset asset = Resources.Load<VisualTreeAsset>(path);
                Assert.That(asset, Is.Not.Null, "Template missing: " + path);
                asset.CloneTree().Query<VisualElement>().ForEach(element =>
                {
                    if (!string.IsNullOrEmpty(element.name))
                    {
                        present.Add(element.name);
                    }
                });
            }

            List<string> missing = new List<string>();
            int seen = 0;
            foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, CaptureFolder), "*.cs"))
            {
                string[] lines = File.ReadAllLines(file);
                for (int line = 0; line < lines.Length; line++)
                {
                    foreach (string name in Names(lines[line]))
                    {
                        seen++;
                        if (!present.Contains(name))
                        {
                            missing.Add("'" + name + "' at " + Path.GetFileName(file) + ":" + (line + 1));
                        }
                    }
                }
            }

            // A scan that reads nothing passes everything, so a moved folder or a broken pattern fails here instead
            Assert.That(seen, Is.GreaterThan(100), "Too few element names read from " + CaptureFolder);
            Assert.That(missing, Is.Empty, "Capture sources name elements the HUD no longer has: "
                + string.Join(", ", missing));
        }
    }
}
