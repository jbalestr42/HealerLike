using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Studio.Editor
{
    // Dirty revisions catch Odin and ordinary Inspector edits, including unsaved assets. Poll only in Play mode;
    // Undo also requests a refresh because a restored serialized value can carry an earlier dirty revision.
    [InitializeOnLoad]
    public static class RenderAssetRefresh
    {
        static readonly Dictionary<string, string> revisions = new Dictionary<string, string>();
        static double nextCheck;
        static bool isForced;

        static RenderAssetRefresh()
        {
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndo;
            RenderGrammarLibraryWindow.OnAssetChanged.AddListener(OnAssetChanged);
        }

        static void OnUndo()
        {
            isForced = true;
        }

        static void OnAssetChanged(Object asset)
        {
            isForced = true;
        }

        static void Update()
        {
            if (!EditorApplication.isPlaying)
            {
                revisions.Clear();
                return;
            }
            if (EditorApplication.timeSinceStartup < nextCheck)
            {
                return;
            }
            nextCheck = EditorApplication.timeSinceStartup + 0.2;
            foreach (RenderManager manager in Object.FindObjectsByType<RenderManager>(FindObjectsSortMode.None))
            {
                string revision = Revision(manager);
                string id = manager.GetEntityId().ToString();
                if (isForced || (revisions.TryGetValue(id, out string previous) && revision != previous))
                {
                    manager.RebuildViews();
                }
                revisions[id] = revision;
            }
            isForced = false;
        }

        public static string Revision(RenderManager manager)
        {
            CreatureLooks looks = manager.creatureLooks;
            LookVocabulary vocabulary = looks ? looks.vocabulary : null;
            return Stamp(vocabulary) + ":" + Stamp(vocabulary ? vocabulary.palette : null);
        }

        static string Stamp(Object asset)
        {
            return asset ? asset.GetEntityId() + ":" + EditorUtility.GetDirtyCount(asset) : "none";
        }
    }
}
