using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    public class AndroidPreviewSettings : IDisposable
    {
        readonly SerializedObject _inputSettings;
        readonly SerializedProperty _inputHandler;
        bool _isDisposed;
        readonly int _oldInputHandler;
        readonly string _oldApplicationIdentifier;
        readonly string _oldProductName;
        readonly string _oldBundleVersion;
        readonly int _oldBundleVersionCode;
        readonly AndroidSdkVersions _oldMinSdkVersion;
        readonly AndroidArchitecture _oldTargetArchitectures;
        readonly ScriptingImplementation _oldScriptingBackend;
        readonly bool _oldUseDefaultGraphicsApis;
        readonly GraphicsDeviceType[] _oldGraphicsApis;
        readonly UIOrientation _oldOrientation;
        readonly bool _oldAutorotatePortrait;
        readonly bool _oldAutorotatePortraitUpsideDown;
        readonly bool _oldAutorotateLandscapeRight;
        readonly bool _oldAutorotateLandscapeLeft;
        readonly bool _oldBuildAppBundle;
        readonly bool _oldExportAndroidProject;
        readonly bool _oldSplitApplicationBinary;
        readonly bool _oldBuildApkPerArchitecture;

        public int inputHandler
        {
            get
            {
                _inputSettings.Update();
                return _inputHandler.intValue;
            }
        }

        public AndroidPreviewSettings()
        {
            System.Reflection.MethodInfo getter = typeof(PlayerSettings).GetMethod("GetSerializedObject",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (getter != null)
            {
                SerializedObject cached = getter.Invoke(null, null) as SerializedObject;
                if (cached != null)
                {
                    // Unity owns the cached wrapper. This scope owns a separate iterator over the same targets.
                    _inputSettings = new SerializedObject(cached.targetObjects);
                }
            }
            if (_inputSettings != null)
            {
                _inputHandler = _inputSettings.FindProperty("activeInputHandler");
            }
            if (_inputHandler == null)
            {
                if (_inputSettings != null)
                {
                    _inputSettings.Dispose();
                }
                throw new InvalidOperationException("Cannot access Unity's activeInputHandler build setting.");
            }
            _oldInputHandler = _inputHandler.intValue;
            _oldApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            _oldProductName = PlayerSettings.productName;
            _oldBundleVersion = PlayerSettings.bundleVersion;
            _oldBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _oldMinSdkVersion = PlayerSettings.Android.minSdkVersion;
            _oldTargetArchitectures = PlayerSettings.Android.targetArchitectures;
            _oldScriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            _oldUseDefaultGraphicsApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            _oldGraphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            _oldOrientation = PlayerSettings.defaultInterfaceOrientation;
            _oldAutorotatePortrait = PlayerSettings.allowedAutorotateToPortrait;
            _oldAutorotatePortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            _oldAutorotateLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            _oldAutorotateLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            _oldBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            _oldExportAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            _oldSplitApplicationBinary = PlayerSettings.Android.splitApplicationBinary;
            _oldBuildApkPerArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;
        }

        public void Apply()
        {
            _inputHandler.intValue = 0;
            _inputSettings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "io.oisif.healerlike.render");
            PlayerSettings.productName = "HealerLike Render";
            PlayerSettings.bundleVersion = StagePreviewBuild.AndroidVersion;
            PlayerSettings.Android.bundleVersionCode = StagePreviewBuild.AndroidVersionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
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

        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                _inputSettings.Update();
                _inputHandler.intValue = _oldInputHandler;
                _inputSettings.ApplyModifiedPropertiesWithoutUndo();
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, _oldApplicationIdentifier);
                PlayerSettings.productName = _oldProductName;
                PlayerSettings.bundleVersion = _oldBundleVersion;
                PlayerSettings.Android.bundleVersionCode = _oldBundleVersionCode;
                PlayerSettings.Android.minSdkVersion = _oldMinSdkVersion;
                PlayerSettings.Android.targetArchitectures = _oldTargetArchitectures;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, _oldScriptingBackend);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, _oldUseDefaultGraphicsApis);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, _oldGraphicsApis);
                PlayerSettings.defaultInterfaceOrientation = _oldOrientation;
                PlayerSettings.allowedAutorotateToPortrait = _oldAutorotatePortrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = _oldAutorotatePortraitUpsideDown;
                PlayerSettings.allowedAutorotateToLandscapeRight = _oldAutorotateLandscapeRight;
                PlayerSettings.allowedAutorotateToLandscapeLeft = _oldAutorotateLandscapeLeft;
                EditorUserBuildSettings.buildAppBundle = _oldBuildAppBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = _oldExportAndroidProject;
                PlayerSettings.Android.splitApplicationBinary = _oldSplitApplicationBinary;
                PlayerSettings.Android.buildApkPerCpuArchitecture = _oldBuildApkPerArchitecture;
            }
            finally
            {
                _inputSettings.Dispose();
            }
        }
    }
}
