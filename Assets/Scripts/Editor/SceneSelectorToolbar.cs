using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

// A dropdown next to the play button to open one of the project scenes (the build ones, then the ones in
// Assets/Scenes), without looking for them in the Project window
[InitializeOnLoad]
public static class SceneSelectorToolbar
{
    const string ElementPath = "HealerLike/Scene Selector";
    const string ScenesFolder = "Assets/Scenes";

    static SceneSelectorToolbar()
    {
        // The dropdown shows the name of the opened scene
        EditorSceneManager.activeSceneChangedInEditMode += (previous, next) => MainToolbar.Refresh(ElementPath);
        EditorApplication.playModeStateChanged += state => MainToolbar.Refresh(ElementPath);
    }

    // Left of the play buttons: the lower the index, the further left in the middle of the toolbar
    [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -100)]
    public static MainToolbarElement CreateSceneSelector()
    {
        Texture2D icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
        MainToolbarContent content = new MainToolbarContent(SceneManager.GetActiveScene().name, icon, "Open a scene");
        return new MainToolbarDropdown(content, ShowScenes);
    }

    static void ShowScenes(Rect dropdownRect)
    {
        GenericMenu menu = new GenericMenu();
        string activeScene = SceneManager.GetActiveScene().path;
        foreach (string scene in GetScenes())
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // A scene can't be opened while playing
                menu.AddDisabledItem(new GUIContent(Path.GetFileNameWithoutExtension(scene)), scene == activeScene);
            }
            else
            {
                menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(scene)), scene == activeScene, () => OpenScene(scene));
            }
        }
        menu.DropDown(dropdownRect);
    }

    // The build scenes first, in their build order, then the other scenes of the project scenes folder
    static List<string> GetScenes()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrEmpty(buildScene.path) && File.Exists(buildScene.path))
            {
                scenes.Add(buildScene.path);
            }
        }

        List<string> folderScenes = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!scenes.Contains(path))
            {
                folderScenes.Add(path);
            }
        }
        folderScenes.Sort();
        scenes.AddRange(folderScenes);
        return scenes;
    }

    static void OpenScene(string path)
    {
        // Lets the user save (or cancel on) the modified scenes first
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}
