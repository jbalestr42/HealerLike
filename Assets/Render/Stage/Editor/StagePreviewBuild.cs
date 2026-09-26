using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    // A player of the render preview with its own scene list, so the project's Build Settings stay untouched.
    // A failure logs and exits with code 1, the non-zero exit code a batchmode run reads.
    public static class StagePreviewBuild
    {
        public const string AndroidVersion = "0.1.4";
        public const int AndroidVersionCode = 5;

        public static readonly string[] Scenes =
        {
            StageSceneAuthoring.ScenePath,
            "Assets/Scenes/Toolkit/MenuToolkit.unity",
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

        public static void BuildAndroidApk()
        {
            string revision = System.Environment.GetEnvironmentVariable("RENDER_BUILD_REVISION");
            if (string.IsNullOrEmpty(revision)
                || !System.Text.RegularExpressions.Regex.IsMatch(revision, @"\A[0-9a-fA-F]{40}\z"))
            {
                Debug.LogError("[StagePreviewBuild] RENDER_BUILD_REVISION must be the exact 40-character Git commit SHA.");
                EditorApplication.Exit(1);
                return;
            }

            string output = System.Environment.GetEnvironmentVariable("RENDER_PLAYER_OUTPUT");
            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine("Builds", "Android", "HealerLike-Render-Android.apk");
            }

            output = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string reportPath = output + ".build.json";

            SerializedObject inputSettings;
            SerializedProperty inputHandler;
            try
            {
                // This is the same current-settings accessor used by Unity 6000.6's BeeBuildPostprocessor.
                System.Reflection.MethodInfo getter = typeof(PlayerSettings).GetMethod("GetSerializedObject",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                inputSettings = getter?.Invoke(null, null) as SerializedObject;
                inputHandler = inputSettings?.FindProperty("activeInputHandler");
                if (inputHandler == null)
                {
                    throw new System.InvalidOperationException("Cannot access Unity's activeInputHandler build setting.");
                }
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                return;
            }
            int oldInputHandler = inputHandler.intValue;

            string oldApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string oldProductName = PlayerSettings.productName;
            string oldBundleVersion = PlayerSettings.bundleVersion;
            int oldBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            AndroidSdkVersions oldMinSdkVersion = PlayerSettings.Android.minSdkVersion;
            AndroidArchitecture oldTargetArchitectures = PlayerSettings.Android.targetArchitectures;
            ScriptingImplementation oldScriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            bool oldUseDefaultGraphicsApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            GraphicsDeviceType[] oldGraphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            UIOrientation oldOrientation = PlayerSettings.defaultInterfaceOrientation;
            bool oldAutorotatePortrait = PlayerSettings.allowedAutorotateToPortrait;
            bool oldAutorotatePortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool oldAutorotateLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            bool oldAutorotateLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            bool oldBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            bool oldExportAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool oldSplitApplicationBinary = PlayerSettings.Android.splitApplicationBinary;
            bool oldBuildApkPerArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;

            bool failed = false;
            try
            {
                inputHandler.intValue = 0;
                inputSettings.ApplyModifiedPropertiesWithoutUndo();
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "io.oisif.healerlike.render");
                PlayerSettings.productName = "HealerLike Render";
                PlayerSettings.bundleVersion = AndroidVersion;
                PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLES3
                });
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = true;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                PlayerSettings.Android.splitApplicationBinary = false;
                PlayerSettings.Android.buildApkPerCpuArchitecture = false;

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = Scenes;
                options.locationPathName = output;
                options.target = BuildTarget.Android;
                // StageLegacyInput has a player compile guard for this exact single-backend configuration.
                options.extraScriptingDefines = new[] { "HEALERLIKE_RENDER_ANDROID_PREVIEW" };
                options.options = BuildOptions.None;
                BuildReport report = BuildPipeline.BuildPlayer(options);
                inputSettings.Update();
                long apkSize = report.summary.result == BuildResult.Succeeded && File.Exists(output)
                    ? new FileInfo(output).Length : 0;
                failed = report.summary.result != BuildResult.Succeeded || apkSize == 0 || inputHandler.intValue != 0;

                AndroidBuildReport buildReport = new AndroidBuildReport
                {
                    revision = revision,
                    unityVersion = Application.unityVersion,
                    target = "Android APK",
                    backend = "IL2CPP",
                    abi = "ARM64",
                    sceneList = Scenes,
                    applicationId = "io.oisif.healerlike.render",
                    applicationLabel = "HealerLike Render",
                    version = AndroidVersion,
                    versionCode = AndroidVersionCode,
                    activeInputHandler = inputHandler.intValue,
                    inputBackend = "Input Manager (Old)",
                    uiInputModule = "StandaloneInputModule",
                    requiredPlayerInputDefine = "ENABLE_LEGACY_INPUT_MANAGER",
                    excludedPlayerInputDefine = "ENABLE_INPUT_SYSTEM",
                    inputDefinesValidatedByCompilation = report.summary.result == BuildResult.Succeeded,
                    buildResult = report.summary.result.ToString(),
                    apkSizeBytes = apkSize,
                    buildSizeBytes = report.summary.totalSize,
                    durationSeconds = report.summary.totalTime.TotalSeconds,
                    outputPath = output
                };
                File.WriteAllText(reportPath, JsonUtility.ToJson(buildReport, true));

                Debug.Log($"[StagePreviewBuild] Android {report.summary.result}, errors {report.summary.totalErrors}, output {output}, report {reportPath}");
            }
            catch (System.Exception error)
            {
                failed = true;
                Debug.LogException(error);
            }
            finally
            {
                inputSettings.Update();
                inputHandler.intValue = oldInputHandler;
                inputSettings.ApplyModifiedPropertiesWithoutUndo();
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldApplicationIdentifier);
                PlayerSettings.productName = oldProductName;
                PlayerSettings.bundleVersion = oldBundleVersion;
                PlayerSettings.Android.bundleVersionCode = oldBundleVersionCode;
                PlayerSettings.Android.minSdkVersion = oldMinSdkVersion;
                PlayerSettings.Android.targetArchitectures = oldTargetArchitectures;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, oldScriptingBackend);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, oldUseDefaultGraphicsApis);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, oldGraphicsApis);
                PlayerSettings.defaultInterfaceOrientation = oldOrientation;
                PlayerSettings.allowedAutorotateToPortrait = oldAutorotatePortrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = oldAutorotatePortraitUpsideDown;
                PlayerSettings.allowedAutorotateToLandscapeRight = oldAutorotateLandscapeRight;
                PlayerSettings.allowedAutorotateToLandscapeLeft = oldAutorotateLandscapeLeft;
                EditorUserBuildSettings.buildAppBundle = oldBuildAppBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExportAndroidProject;
                PlayerSettings.Android.splitApplicationBinary = oldSplitApplicationBinary;
                PlayerSettings.Android.buildApkPerCpuArchitecture = oldBuildApkPerArchitecture;
            }

            if (failed)
            {
                Debug.LogError("[StagePreviewBuild] The Android render preview build failed, did not produce an APK, or did not retain the required legacy input backend.");
                EditorApplication.Exit(1);
            }
        }

        [System.Serializable]
        private sealed class AndroidBuildReport
        {
            public string revision;
            public string unityVersion;
            public string target;
            public string backend;
            public string abi;
            public string[] sceneList;
            public string applicationId;
            public string applicationLabel;
            public string version;
            public int versionCode;
            public int activeInputHandler;
            public string inputBackend;
            public string uiInputModule;
            public string requiredPlayerInputDefine;
            public string excludedPlayerInputDefine;
            public bool inputDefinesValidatedByCompilation;
            public string buildResult;
            public long apkSizeBytes;
            public ulong buildSizeBytes;
            public double durationSeconds;
            public string outputPath;
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
