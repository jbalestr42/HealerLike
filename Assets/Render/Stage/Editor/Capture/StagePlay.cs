using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Batchmode play sessions of RenderStage.unity. The run component is added once play mode starts,
    // and the editor exits with the code the run set.
    [InitializeOnLoad]
    public static class StagePlay
    {
        public static readonly string ScenePath = "Assets/Render/Stage/RenderStage.unity";
        public static readonly string CaptureFolder = "/Users/fc/Documents/healerlike-render-specs/captures/";

        static readonly string modeKey = "StagePlay.Mode";
        static readonly string codeKey = "StagePlay.Code";
        static readonly string deadlineKey = "StagePlay.Deadline";
        static EditorWindow _gameView;
        static AStageRun _run;

        static StagePlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        public static void Enter(string mode, float seconds)
        {
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetString(modeKey, mode);
            SessionState.SetInt(codeKey, 1);
            SessionState.SetFloat(deadlineKey, (float)EditorApplication.timeSinceStartup + seconds);
            EditorApplication.isPlaying = true;
        }

        // Called by the run when it is done
        public static void Finish(bool isPassed)
        {
            _run = null;
            SessionState.SetInt(codeKey, isPassed ? 0 : 1);
            EditorApplication.isPlaying = false;
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
                _run = mode == "smoke" ? new StageSmokeRun() : (AStageRun)new StageCaptureRun(mode == "landscape");
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
                Debug.LogError("[StagePlay] The session ran past its deadline.");
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(2);
                return;
            }

            // Batchmode has no visible Game view, the repaint is what lets end of frame waits resume
            if (EditorApplication.isPlaying)
            {
                RepaintGameView();
                if (_run != null)
                {
                    _run.Step();
                }
            }
        }

        static void RepaintGameView()
        {
            if (_gameView == null)
            {
                Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                _gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
            }

            _gameView.Repaint();
        }
    }
}
