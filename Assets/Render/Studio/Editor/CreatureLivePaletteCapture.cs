using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Studio.Editor
{
    // Separate session key so this opt-in Studio capture does not alter the shared stage capture modes.
    [InitializeOnLoad]
    public static class CreatureLivePaletteCapture
    {
        const string key = "CreatureLivePaletteCapture.Running";
        static CreatureLivePaletteRun run;

        static CreatureLivePaletteCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += Watch;
        }

        public static void Capture()
        {
            EditorSceneManager.OpenScene(StageSceneAuthoring.ScenePath);
            SessionState.SetBool(key, true);
            SessionState.SetInt(key + ".Code", 1);
            SessionState.SetFloat(key + ".Deadline", (float)EditorApplication.timeSinceStartup + 120f);
            EditorApplication.isPlaying = true;
        }

        public static void Finish(bool passed)
        {
            if (run != null)
            {
                EditorApplication.update -= run.Step;
                run.Restore();
                run = null;
            }
            SessionState.SetInt(key + ".Code", passed ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(key, false))
            {
                return;
            }
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                run = new CreatureLivePaletteRun();
                run.Begin();
                EditorApplication.update += run.Step;
            }
            if (state == PlayModeStateChange.ExitingPlayMode && run != null)
            {
                run.Restore();
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                int code = SessionState.GetInt(key + ".Code", 1);
                SessionState.SetBool(key, false);
                EditorApplication.Exit(code);
            }
        }

        static void Watch()
        {
            if (SessionState.GetBool(key, false)
                && EditorApplication.timeSinceStartup > SessionState.GetFloat(key + ".Deadline", 0f))
            {
                Debug.LogError("[CreatureLivePaletteCapture] Timed out waiting for the live asset edit.");
                Finish(false);
            }
        }
    }
}
