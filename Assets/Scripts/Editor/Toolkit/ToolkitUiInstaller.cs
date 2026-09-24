using System;
using System.Linq;
using HealerLike.UI.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Editor.Toolkit
{
    /// <summary>Explicit opt-in: existing scenes and their legacy UI remain intact.</summary>
    public static class ToolkitUiInstaller
    {
        public const string GameplayPath = "Assets/Scenes/Toolkit/MainToolkit.unity";
        public const string MenuPath = "Assets/Scenes/Toolkit/MenuToolkit.unity";

        [MenuItem("HealerLike/UI Toolkit/Install in Current Scene")]
        public static void InstallInCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Open a scene before installing UI Toolkit.");
            if (!scene.path.StartsWith("Assets/Scenes/Toolkit/", StringComparison.Ordinal))
                throw new InvalidOperationException("Install only in a Toolkit scene copy. Use Create Separate Demo Scenes first.");
            Install(scene, "MainToolkit", "MenuToolkit");
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("HealerLike/UI Toolkit/Create Separate Demo Scenes")]
        public static void CreateDemoScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scenes/Toolkit"))
                    AssetDatabase.CreateFolder("Assets/Scenes", "Toolkit");
                CreateScene("Assets/Scenes/Main.unity", GameplayPath);
                CreateScene("Assets/Scenes/MenuScene.unity", MenuPath);
                Debug.Log("UI Toolkit demo ready: open " + MenuPath + " and press Play. Original scenes are preserved.");
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void CreateScene(string source, string destination)
        {
            // Existing demo scenes may contain user design changes: never overwrite them.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) == null && !AssetDatabase.CopyAsset(source, destination))
                throw new InvalidOperationException("Could not copy scene " + source);
            var scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Single);
            Install(scene, "MainToolkit", "MenuToolkit");
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save scene " + destination);
        }

        static void Install(Scene scene, string gameplay, string menu)
        {
            var ui = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ToolkitGameUI>(true)).FirstOrDefault();
            if (ui == null)
            {
                var host = new GameObject("UI Toolkit");
                SceneManager.MoveGameObjectToScene(host, scene);
                Undo.RegisterCreatedObjectUndo(host, "Install UI Toolkit");
                ui = Undo.AddComponent<ToolkitGameUI>(host);
            }
            Undo.RecordObject(ui, "Configure UI Toolkit navigation");
            ui.ConfigureScenes(gameplay, menu);
            EditorUtility.SetDirty(ui);
        }
    }
}
