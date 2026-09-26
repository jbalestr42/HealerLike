using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit opt-in: the existing scenes and their legacy UI stay intact
public static class ToolkitUiInstaller
{
    static readonly string toolkitScenesFolder = "Assets/Scenes/Toolkit/";

    [MenuItem("Tools/UI Toolkit/Install in Current Scene")]
    public static void InstallInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[ToolkitUiInstaller] Open a scene before installing UI Toolkit");
            return;
        }

        if (!scene.path.StartsWith(toolkitScenesFolder, System.StringComparison.Ordinal))
        {
            Debug.LogError("[ToolkitUiInstaller] Install only in a Toolkit scene copy, "
                + "use Create Separate Demo Scenes first");
            return;
        }

        Install(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/UI Toolkit/Create Separate Demo Scenes")]
    public static void CreateDemoScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Toolkit"))
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "Toolkit");
        }

        if (CreateScene("Assets/Scenes/Main.unity", ToolkitSceneNavigation.GameplayPath))
        {
            CreateScene("Assets/Scenes/MenuScene.unity", ToolkitSceneNavigation.MenuPath);
        }

        if (setup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    // Explicit maintenance of our disposable demo copy after an upstream scene change.
    // The source scene and its serialized gameplay wiring are never edited.
    [MenuItem("Tools/UI Toolkit/Refresh Gameplay Demo from Main")]
    public static void RefreshGameplayDemo()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        System.IO.File.Copy("Assets/Scenes/Main.unity", ToolkitSceneNavigation.GameplayPath, true);
        AssetDatabase.ImportAsset(ToolkitSceneNavigation.GameplayPath, ImportAssetOptions.ForceUpdate);
        Scene scene = EditorSceneManager.OpenScene(ToolkitSceneNavigation.GameplayPath, OpenSceneMode.Single);
        Install(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new System.InvalidOperationException("Could not save the refreshed Toolkit gameplay demo");
        if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
    }

    static bool CreateScene(string source, string destination)
    {
        // Existing demo scenes may contain design changes: never overwrite them
        bool isMissing = AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) == null;
        if (isMissing && !AssetDatabase.CopyAsset(source, destination))
        {
            Debug.LogError($"[ToolkitUiInstaller] Could not copy scene {source}");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Single);
        Install(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError($"[ToolkitUiInstaller] Could not save scene {destination}");
            return false;
        }

        return true;
    }

    static void Install(Scene scene)
    {
        ToolkitGameUI gameUI = FindGameUI(scene);
        if (gameUI == null)
        {
            GameObject host = new GameObject("UI Toolkit");
            SceneManager.MoveGameObjectToScene(host, scene);
            Undo.RegisterCreatedObjectUndo(host, "Install UI Toolkit");
            gameUI = Undo.AddComponent<ToolkitGameUI>(host);
        }

        Undo.RecordObject(gameUI, "Configure UI Toolkit navigation");
        gameUI.gameplayScene = ToolkitSceneNavigation.GameplayScene;
        gameUI.menuScene = ToolkitSceneNavigation.MenuScene;
        EditorUtility.SetDirty(gameUI);
    }

    static ToolkitGameUI FindGameUI(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ToolkitGameUI[] gameUIs = root.GetComponentsInChildren<ToolkitGameUI>(true);
            if (gameUIs.Length > 0)
            {
                return gameUIs[0];
            }
        }

        return null;
    }
}
