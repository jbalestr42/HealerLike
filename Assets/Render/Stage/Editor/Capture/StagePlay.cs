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

        // The mode of the session being played, empty outside a batchmode play session
        public static string Mode { get { return SessionState.GetString(modeKey, ""); } }

        public static string ReadRevision()
        {
            string revision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION");
            return revision != null ? revision : "unspecified";
        }

        // The scene a mode's session boots beside the stage. A plain boot opens the Toolkit menu, so every run that
        // plays Main from the start asks for it; the sandbox runs boot the sandbox, and the menu boot run keeps the
        // plain boot (null: nothing selected).
        public static string BootTarget(string mode)
        {
            if (SandboxCaptureRun.BootsSandbox(mode))
            {
                return StageTarget.SandboxPath;
            }

            if (mode == ClassSelectCaptureRun.BootMode)
            {
                return null;
            }

            return StageTarget.MainPath;
        }

        // A play session starts with no target (StageTarget resets at SubsystemRegistration), so a batchmode session
        // selects its boot scene after that reset and before StageLauncher.Start reads it
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SelectBootTarget()
        {
            string mode = Mode;
            if (mode == "")
            {
                return;
            }

            string path = BootTarget(mode);
            if (path != null)
            {
                StageTarget.Select(path);
            }

            Debug.Log("[StagePlay] Mode " + mode + " boots " + StageTarget.scenePath
                + (StageTarget.isSelected ? "" : " (plain boot)"));
        }

        // RENDER_SIMULATE_NO_VERTEX_BUFFERS=1 plays the session as a device without vertex storage buffers (an
        // OpenGL ES 3.1 phone at its guaranteed minimum), so a capture shows the board with the ground simulation
        // and the compute grass off. Read at every play start, since entering play mode reloads the domain.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SimulateDeviceLimits()
        {
            bool isSimulated = System.Environment.GetEnvironmentVariable("RENDER_SIMULATE_NO_VERTEX_BUFFERS") == "1";
            HealerLike.Render.Grass.VertexStorageBuffers.isForcedUnavailable = isSimulated;
            if (isSimulated)
            {
                Debug.Log("[StagePlay] Simulating a device without vertex storage buffers");
            }
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
            if (mode == "compact-ui") return new StageCompactRun();
            if (mode == "selection-facing") return new SelectionFacingRun();
            if (mode == "spell-icons") return new SpellIconRun();
            if (mode == "spell-polish") return new SpellPolishRun();
            if (mode == "spell-readability") return new SpellPolishRun(true);
            if (mode == "spell-sources") return new SpellSourceRun();
            if (mode == FieldVariantRun.Mode) return new FieldVariantRun();
            if (mode == SandboxCaptureRun.BootMode) return new SandboxCaptureRun(false);
            if (mode == SandboxCaptureRun.MenuMode) return new SandboxCaptureRun(true);
            if (mode == SandboxCaptureRun.BackMode) return new SandboxCaptureRun(false, true);
            if (mode == ClassSelectCaptureRun.Mode) return new ClassSelectCaptureRun();
            if (mode == ClassSelectCaptureRun.BootMode) return new ClassSelectCaptureRun(true);

            if (mode == StageEventRoomRun.Mode)
            {
                return new StageEventRoomRun();
            }

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
                if (mode == "mobile-interface" || mode == "creature-presentation" || mode == "expedition-map"
                    || mode == "spell-sources" || mode == "selection-facing" || mode == "compact-ui"
                    || mode == FieldVariantRun.Mode || mode == StageEventRoomRun.Mode)
                {
                    // Screen and pointer coordinates must be read inside a game frame, not Editor.update. The field
                    // variants read the game camera back too, so they step after the grass's indirect draw is queued.
                    GameObject host = new GameObject("Stage capture frame");
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    _frame = mode == "creature-presentation" || mode == FieldVariantRun.Mode
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
                LogLeakedRenderers();
                int code = SessionState.GetInt(codeKey, 1);
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(code);
            }
        }

        // What play mode left behind in the edit-mode scene: every renderer outside an asset that is not the
        // Editor's own handles, with its root, flags, material and shader. The next play session would draw these.
        public static int LogLeakedRenderers()
        {
            int leaked = 0;
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || UnityEditor.EditorUtility.IsPersistent(renderer)
                    || renderer.transform.root.name == "HandlesGO")
                {
                    continue;
                }

                leaked++;
                Material material = renderer.sharedMaterial;
                Debug.Log("[StagePlay] Left after play: " + renderer.transform.root.name + " > " + renderer.name
                    + " scene '" + renderer.gameObject.scene.name + "' flags " + renderer.gameObject.hideFlags
                    + " root flags " + renderer.transform.root.gameObject.hideFlags + " active "
                    + renderer.gameObject.activeInHierarchy + " at " + renderer.transform.position + " material "
                    + (material == null ? "NULL" : material.name + "/" + (material.shader ? material.shader.name : "none")));
            }

            Debug.Log("[StagePlay] Renderers left after play: " + leaked);
            return leaked;
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
