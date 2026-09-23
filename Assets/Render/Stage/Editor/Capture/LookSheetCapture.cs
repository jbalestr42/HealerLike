using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Batchmode play sessions that write the look grammar's sheets and the silhouette overlap list:
    // -executeMethod HealerLike.Render.Stage.LookSheetCapture.All runs units, effects and the list in one session,
    // .Units and .Effects run one of them. The editor exits with the run's code.
    [InitializeOnLoad]
    public static class LookSheetCapture
    {
        static readonly string modeKey = "LookSheetCapture.Mode";
        static readonly string codeKey = "LookSheetCapture.Code";
        static readonly string deadlineKey = "LookSheetCapture.Deadline";
        static EditorWindow _gameView;
        static AStageRun _run;

        static LookSheetCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        [MenuItem("Tools/Render/Capture Look Sheet")]
        public static void Units()
        {
            Enter("units", 600f);
        }

        [MenuItem("Tools/Render/Capture Effect Sheet")]
        public static void Effects()
        {
            Enter("effects", 600f);
        }

        [MenuItem("Tools/Render/Capture Every Look Sheet")]
        public static void All()
        {
            Enter("all", 1200f);
        }

        public static void Finish(bool isPassed)
        {
            _run = null;
            SessionState.SetInt(codeKey, isPassed ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        static void Enter(string mode, float seconds)
        {
            EditorSceneManager.OpenScene(StagePlay.ScenePath);
            SessionState.SetString(modeKey, mode);
            SessionState.SetInt(codeKey, 1);
            SessionState.SetFloat(deadlineKey, (float)EditorApplication.timeSinceStartup + seconds);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            string mode = SessionState.GetString(modeKey, "");
            if (mode == "")
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                _run = new LookSheetRun(mode != "effects", mode != "units");
                _run.Begin();
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                int code = SessionState.GetInt(codeKey, 1);
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(code);
            }
        }

        static void OnUpdate()
        {
            if (SessionState.GetString(modeKey, "") == "")
            {
                return;
            }

            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(deadlineKey, 0f))
            {
                Debug.LogError("[LookSheetCapture] The session ran past its deadline.");
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(2);
                return;
            }

            // Batchmode has no visible Game view, the repaint is what lets end of frame waits resume
            if (EditorApplication.isPlaying)
            {
                if (_gameView == null)
                {
                    Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                    _gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                }

                _gameView.Repaint();
                if (_run != null)
                {
                    _run.Step();
                }
            }
        }
    }
}
