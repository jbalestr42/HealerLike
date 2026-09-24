using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The studios' drafts as EditorPrefs keeps them, one JSON object under one key
    public static class StudioPrefs
    {
        // The kept JSON, or null when nothing was kept. A value that is not one whole JSON object, a write cut
        // short, is dropped with an error rather than handed to JsonUtility, which would throw on it
        public static string ReadJson(string key)
        {
            if (!EditorPrefs.HasKey(key))
            {
                return null;
            }

            string json = EditorPrefs.GetString(key).Trim();
            if (!IsWholeObject(json))
            {
                Debug.LogError($"[StudioPrefs] Dropped the unreadable drafts under {key}");
                EditorPrefs.DeleteKey(key);
                return null;
            }
            return json;
        }

        // Starts with a brace and closes every brace and bracket it opens, outside strings, at its last character
        public static bool IsWholeObject(string json)
        {
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
            {
                return false;
            }

            int depth = 0;
            bool isInString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (isInString)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        isInString = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    isInString = true;
                }
                else if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth < 0 || (depth == 0 && i != json.Length - 1))
                    {
                        return false;
                    }
                }
            }
            return depth == 0 && !isInString;
        }
    }
}
