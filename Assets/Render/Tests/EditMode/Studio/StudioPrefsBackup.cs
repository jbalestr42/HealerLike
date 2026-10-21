using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio
{

// Keeps the author's own studio drafts out of the way of a test: Save clears them, Restore puts them back
public class StudioPrefsBackup
{
    readonly Dictionary<string, string> _values = new Dictionary<string, string>();

    static string[] Keys()
    {
        return new string[]
        {
            "HealerLike.SpellStudio.Drafts." + Application.dataPath,
            "HealerLike.CreatureStudio.Drafts." + Application.dataPath,
            "HealerLike.CreatureStudio.GrammarDrafts." + Application.dataPath
        };
    }

    public void Save()
    {
        _values.Clear();
        foreach (string key in Keys())
        {
            if (EditorPrefs.HasKey(key))
            {
                _values[key] = EditorPrefs.GetString(key);
            }
            EditorPrefs.DeleteKey(key);
        }
    }

    public void Restore()
    {
        foreach (string key in Keys())
        {
            if (_values.ContainsKey(key))
            {
                EditorPrefs.SetString(key, _values[key]);
            }
            else
            {
                EditorPrefs.DeleteKey(key);
            }
        }
    }
}

}
