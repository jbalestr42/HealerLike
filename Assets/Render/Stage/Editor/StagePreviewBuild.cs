using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A player of the render preview with its own scene list, so the project's Build Settings stay untouched.
    // A failure logs and exits with code 1, the non-zero exit code a batchmode run reads.
    public static class StagePreviewBuild
    {
        public static readonly string[] Scenes =
        {
            StageSceneAuthoring.ScenePath,
            "Assets/Scenes/MenuScene.unity",
            "Assets/Scenes/Main.unity"
        };

        [MenuItem("Tools/Render/Build Render Preview")]
        public static void BuildPlayer()
        {
            string output = System.Environment.GetEnvironmentVariable("RENDER_PLAYER_OUTPUT");
            if (string.IsNullOrEmpty(output))
            {
                output = "/tmp/HealerLikeRender.app";
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = Scenes;
            options.locationPathName = output;
            options.target = BuildTarget.StandaloneOSX;
            options.options = BuildOptions.Development;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log(
                $"[StagePreviewBuild] {report.summary.result}, errors {report.summary.totalErrors}, output {output}");
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[StagePreviewBuild] The render preview player build failed.");
                EditorApplication.Exit(1);
                return;
            }
        }

        // Compiles the player scripts without building or changing a scene
        public static void VerifyPlayerCompilation()
        {
            ScriptCompilationSettings settings = new ScriptCompilationSettings();
            settings.target = BuildTarget.StandaloneOSX;
            settings.group = BuildTargetGroup.Standalone;
            string output = Path.Combine(Path.GetTempPath(), "hl-player-scripts");
            Directory.CreateDirectory(output);
            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, output);

            bool hasRender = false;
            bool hasEditorCode = false;
            if (result.assemblies != null)
            {
                foreach (string assembly in result.assemblies)
                {
                    string name = Path.GetFileName(assembly);
                    hasRender |= name == "HealerLike.Render.dll";
                    hasEditorCode |= name.StartsWith("HealerLike.Render.") && name.Contains(".Editor");
                }
            }

            if (result.typeDB != null)
            {
                result.typeDB.Dispose();
            }

            if (!hasRender)
            {
                Debug.LogError("[StagePreviewBuild] The player has no render assembly.");
                EditorApplication.Exit(1);
                return;
            }

            if (hasEditorCode)
            {
                Debug.LogError("[StagePreviewBuild] Render editor code was included in the player.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[StagePreviewBuild] StandaloneOSX player scripts compiled, render editor code excluded.");
        }
    }
}
