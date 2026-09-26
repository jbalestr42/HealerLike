using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // JSON syntax is checked before JsonUtility restores Unity fields and references.
    public static class StudioPrefs
    {
        public static string ReadJson(string key)
        {
            if (!EditorPrefs.HasKey(key))
            {
                return null;
            }

            string json = EditorPrefs.GetString(key).Trim();
            if (!IsWholeObject(json))
            {
                Discard(key);
                return null;
            }
            return json;
        }

        public static bool TryRead<Collection>(string key, out Collection collection)
            where Collection : class
        {
            collection = null;
            string json = ReadJson(key);
            if (json == null)
            {
                return false;
            }

            try
            {
                collection = JsonUtility.FromJson<Collection>(json);
                if (collection != null)
                {
                    return true;
                }
            }
            catch (ArgumentException)
            {
                // A syntactically valid document can still contain the wrong field types.
            }
            Discard(key);
            return false;
        }

        public static bool TryDraft<Draft>(string json, out Draft draft) where Draft : ScriptableObject
        {
            draft = null;
            if (!IsWholeObject(json))
            {
                return false;
            }

            Draft created = ScriptableObject.CreateInstance<Draft>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, created);
                draft = created;
                return true;
            }
            catch (ArgumentException)
            {
                UnityEngine.Object.DestroyImmediate(created);
                return false;
            }
        }

        public static void Discard(string key)
        {
            Debug.LogError($"[StudioPrefs] Dropped the unreadable drafts under {key}");
            EditorPrefs.DeleteKey(key);
        }

        public static bool IsWholeObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            json = json.Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
            {
                return false;
            }

            try
            {
                return JObject.Parse(json) != null;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }
}
