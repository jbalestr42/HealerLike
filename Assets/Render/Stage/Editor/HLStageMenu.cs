using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Opens the stage scenes, and builds the render-owned menu copy whose Start loads the stage instead of Main.
    // Julien's MenuScene (MainMenu.StartGame -> SceneManager.LoadScene("Main")) and Build Settings are never modified.
    // Way back: the stage's GameOver restart button calls SceneManager.LoadScene("MenuScene"), i.e. Julien's menu (in Build Settings), not this copy.
    public static class HLStageMenu
    {
        const string SourceMenu = "Assets/Scenes/MenuScene.unity";
        public const string MenuPath = HLStageBuilder.Root + "HLStageMenu.unity";
        [MenuItem("HealerLike/Render/Open Stage")]
        public static void OpenStage() => Open(HLStageBuilder.ScenePath);
        [MenuItem("HealerLike/Render/Open Stage Menu")]
        public static void OpenStageMenu() => Open(MenuPath);
        static void Open(string path)
        {
            if (!File.Exists(path)) { Debug.LogWarning("HL stage: missing " + path + (path == MenuPath ? "; run HealerLike/Render/Build Stage Menu Scene" : "; run the stage build")); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
        [MenuItem("HealerLike/Render/Build Stage Menu Scene")]
        public static void BuildMenuScene()
        {
            if (!File.Exists(SourceMenu)) { Debug.LogError("HL stage menu: missing " + SourceMenu); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!string.Equals(HLStageBuilder.ScenePath, HLStageSceneLoader.ScenePath)) Debug.LogWarning($"HL stage menu: loader path {HLStageSceneLoader.ScenePath} differs from stage {HLStageBuilder.ScenePath}");
            bool fresh = !File.Exists(MenuPath);
            File.Copy(SourceMenu, MenuPath, true);
            if (fresh || !File.Exists(MenuPath + ".meta")) File.WriteAllText(MenuPath + ".meta", "fileFormatVersion: 2\nguid: " + Guid.NewGuid().ToString("N") + "\n");
            AssetDatabase.ImportAsset(MenuPath, ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            int rewired = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    if (!IsStart(button)) continue;
                    for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
                    var loader = button.GetComponent<HLStageSceneLoader>();
                    if (!loader) loader = button.gameObject.AddComponent<HLStageSceneLoader>();
                    UnityEventTools.AddPersistentListener(button.onClick, loader.Load);
                    EditorUtility.SetDirty(button); rewired++;
                    Debug.Log($"HL stage menu: {button.name} now calls HLStageSceneLoader.Load -> {HLStageSceneLoader.ScenePath}");
                }
            if (rewired == 0) Debug.LogError("HL stage menu: no Start button (MainMenu.StartGame listener or StartGame object) found in " + MenuPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuPath);
            AssetDatabase.Refresh();
        }
        // Julien's Start button: GameObject "StartGame" whose persistent onClick calls MainMenu.StartGame.
        static bool IsStart(UnityEngine.UI.Button button)
        {
            if (button.name == "StartGame") return true;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "StartGame" && button.onClick.GetPersistentTarget(i) is MainMenu) return true;
            return false;
        }
    }
}
