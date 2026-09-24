using System;
using System.Collections.Generic;
using UnityEditor;
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
            foreach (CreatureRosterRow row in _rows)
            {
                row.Rebuild(vocabulary, side);
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
