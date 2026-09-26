using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Batchmode play sessions of RenderStage.unity. The mode picks the run, which is added once play mode starts,
    // and the editor exits with the code the run set. The mode, code and deadline live in SessionState, since
    // entering play mode reloads the domain.
    [InitializeOnLoad]
    public static class StagePlay
    {
        static StageCaptureFrame _frame;
        static AStageRun _activeRun;
        static readonly string modeKey = "StagePlay.Mode";
        static readonly string codeKey = "StagePlay.Code";
        static readonly string deadlineKey = "StagePlay.Deadline";
        static readonly string captureFolderVariable = "RENDER_CAPTURE_DIR";

        // RENDER_CAPTURE_DIR when set, else Logs/Captures/ under the project, which the project ignores
        public static string CaptureFolder
        {
            get
            {
                string folder = System.Environment.GetEnvironmentVariable(captureFolderVariable);
                if (string.IsNullOrEmpty(folder))
                {
                    folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Captures");
                }

                if (!folder.EndsWith("/"))
                {
                    folder += "/";
                }

                return folder;
            }
        }

        public static string ReadRevision()
        {
            string revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION");
            return revision != null ? revision : "unspecified";
        }

        static StagePlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        public static void Enter(string mode, float seconds)
        {
            EditorSceneManager.OpenScene(StageSceneAuthoring.ScenePath);
            SessionState.SetString(modeKey, mode);
            SessionState.SetInt(codeKey, 1);
            SessionState.SetFloat(deadlineKey, (float)EditorApplication.timeSinceStartup + seconds);
            EditorApplication.isPlaying = true;
        }

        // Called by the run when it is done
        public static void Finish(AStageRun run, bool isPassed)
        {
            EditorApplication.update -= run.Step;
            run.Stop();
            if (ReferenceEquals(_activeRun, run))
            {
                _activeRun = null;
            }

            if (_frame != null)
            {
                _frame.onFrame = null;
                UnityEngine.Object.Destroy(_frame.gameObject);
                _frame = null;
            }

            SessionState.SetInt(codeKey, isPassed ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        // The run each mode names, the one place a capture registers
        static AStageRun Create(string mode)
        {
            if (mode == "spell-sources") return new SpellSourceRun();

            if (mode == "expedition-map")
            {
                return new StageMapRun();
            }

            if (mode == "creature-presentation")
            {
                return new StagePresentationRun();
            }

            if (mode == "mobile-interface")
            {
                return new StageInterfaceRun();
            }

            if (mode == "smoke")
            {
                return new StageSmokeRun();
            }

            if (mode == "portrait" || mode == "landscape")
            {
                return new StageCaptureRun(mode == "landscape");
            }

            if (mode == "ground")
            {
                return new GroundCaptureRun();
            }

            if (mode == "grassbench")
            {
                return new GrassBenchRun();
            }

            if (mode == "grasslab")
            {
                return new GrassLabRun();
            }

            if (mode == "grassbattle")
            {
                return new GrassBattleRun();
            }

            if (mode == "grassjitter")
            {
                return new GrassJitterRun();
            }
            if (mode == "offscreen-player")
            {
                return new OffscreenPlayerRun();
            }

            if (mode == "motion")
            {
                return new StageMotionRun();
            }

            return new LookSheetRun(mode != "effects", mode != "units");
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                StopActive();
            }

            string mode = SessionState.GetString(modeKey, "");
            if (mode == "")
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                AStageRun run = Create(mode);
                _activeRun = run;
                if (mode == "mobile-interface" || mode == "creature-presentation" || mode == "expedition-map")
                {
                    // Screen and pointer coordinates must be read inside a game frame, not Editor.update.
                    GameObject host = new GameObject("Stage capture frame");
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    _frame = mode == "creature-presentation"
                        ? host.AddComponent<StagePresentationFrame>() : host.AddComponent<StageCaptureFrame>();
                    _frame.onFrame = run.Step;
                }
                else
                {
                    EditorApplication.update += run.Step;
                }

                run.Begin();
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                int code = SessionState.GetInt(codeKey, 1);
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(code);
            }
        }

        static void StopActive()
        {
            AStageRun run = _activeRun;
            _activeRun = null;
            if (_frame != null)
            {
                _frame.onFrame = null;
            }

            if (run != null)
            {
                EditorApplication.update -= run.Step;
                run.Stop();
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
                StopActive();
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(2);
                return;
            }

            // Batchmode has no visible Game view, the repaint is what lets end of frame waits resume
            if (EditorApplication.isPlaying)
            {
                Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                EditorWindow.GetWindow(gameViewType, false, null, false).Repaint();
            }
        }
    }
}
