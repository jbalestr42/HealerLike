using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    public class CreatureRoster : IDisposable
    {
        readonly List<CreatureRosterRow> _rows = new List<CreatureRosterRow>();
        public IReadOnlyList<CreatureRosterRow> rows { get { return _rows; } }

        public void Reload(LookVocabulary vocabulary, Entity.EntityType side)
        {
            Dispose();
            List<EntityData> entities = new List<EntityData>();
            foreach (string guid in AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" }))
            {
                EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data)
                {
                    entities.Add(data);
                }
            }
            entities.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b),
                StringComparison.Ordinal));
            foreach (EntityData data in entities)
            {
                CreatureRosterRow row = new CreatureRosterRow();
                row.Init(data);
                _rows.Add(row);
            }
            Rebuild(vocabulary, side);
        }

        public void Rebuild(LookVocabulary vocabulary, Entity.EntityType side)
        {
            Bounds? bounds = null;
            foreach (CreatureRosterRow row in _rows)
            {
                row.Rebuild(vocabulary, side);
                if (row.preview.Sample(row.recipe, 1.25f) != null)
                {
                    Bounds content = row.preview.GetContentBounds();
                    if (bounds.HasValue)
                    {
                        Bounds combined = bounds.Value;
                        combined.Encapsulate(content);
                        bounds = combined;
                    }
                    else
                    {
                        bounds = content;
                    }
                }
            }
            foreach (CreatureRosterRow row in _rows)
            {
                row.preview.framingBounds = bounds;
            }
        }

        public void Dispose()
        {
            foreach (CreatureRosterRow row in _rows)
            {
                row.Dispose();
            }
            _rows.Clear();
        }
    }
}
