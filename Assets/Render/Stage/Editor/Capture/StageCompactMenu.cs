using System;
using System.Reflection;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public static class StageCompactMenu
    {
        // Unity's new-project Search startup must happen in Edit mode. Keep any engine failures in the log.
        public static void InitializeSearch()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("UnityEditor.Search.SearchInit");
                if (type == null) continue;
                type.GetMethod("IndexationOnStartup", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(null, null);
                return;
            }
            throw new InvalidOperationException("Unity Search initialization entry point not found");
        }
        public static void Capture()
        {
            InitializeSearch();
            StageCaptureTheme.Prepare();
            EditorApplication.delayCall += () => StagePlay.Enter("compact-ui", 420);
        }
    }
}
