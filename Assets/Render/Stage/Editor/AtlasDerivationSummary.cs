using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using Document = HealerLike.Render.Stage.AtlasDerivationDump.Document;
using Vocabulary = HealerLike.Render.Stage.AtlasDerivationDump.Vocabulary;
using Inputs = HealerLike.Render.Stage.AtlasDerivationDump.Inputs;
using Channels = HealerLike.Render.Stage.AtlasDerivationDump.Channels;
using EntityRow = HealerLike.Render.Stage.AtlasDerivationDump.EntityRow;
using HandlerRow = HealerLike.Render.Stage.AtlasDerivationDump.HandlerRow;
using BuffInput = HealerLike.Render.Stage.AtlasDerivationDump.BuffInput;
using CharacterRow = HealerLike.Render.Stage.AtlasDerivationDump.CharacterRow;
using HeadInput = HealerLike.Render.Stage.AtlasDerivationDump.HeadInput;
using ProjectileRow = HealerLike.Render.Stage.AtlasDerivationDump.ProjectileRow;
using Distinct = HealerLike.Render.Stage.AtlasDerivationDump.Distinct;

namespace HealerLike.Render.Stage
{
    public static class AtlasDerivationSummary
    {
        public static void Append(Document document)
        {
            foreach (string side in new[] { "Player", "Computer" })
            {
                EntityRow[] rows = document.entities.Where(row => row.side == side).ToArray();
                foreach (System.Reflection.FieldInfo field in typeof(Channels).GetFields())
                {
                    Summary(document, "entities", side, field.Name,
                        rows.Select(row => (string)field.GetValue(row.channels)));
                }
                Summary(document, "entities", side, "effectiveReach",
                    rows.Select(row => row.effectiveReach.ToString("R",
                        System.Globalization.CultureInfo.InvariantCulture)));
            }
            foreach (string side in new[] { "Same", "Opposing" })
            {
                HandlerRow[] rows = document.handlers.Where(row => row.side == side).ToArray();
                foreach (string field in new[] { "family", "group", "tempo", "element" })
                {
                    Summary(document, "handlers", side, field,
                        rows.Select(row => (string)typeof(HandlerRow).GetField(field).GetValue(row)));
                }
                Summary(document, "handlers", side, "periodSeconds",
                    rows.Select(row => row.periodSeconds.ToString("R",
                        System.Globalization.CultureInfo.InvariantCulture)));
            }
            Summary(document, "projectiles", "", "head", document.projectiles.Select(row => row.head));
            Summary(document, "projectiles", "", "delivery", document.projectiles.Select(row => row.delivery));
            Summary(document, "characters", "", "view", document.characters.Select(row => row.view));
        }

        static void Summary(Document document, string collection, string side, string channel,
            IEnumerable<string> values)
        {
            string[] distinct = values.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
            document.summary.Add(new Distinct { collection = collection, side = side, channel = channel,
                distinctCount = distinct.Length, isConstant = distinct.Length == 1, values = distinct });
        }

    }
}
