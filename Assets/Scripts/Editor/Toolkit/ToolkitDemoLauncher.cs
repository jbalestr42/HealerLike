using HealerLike.UI.Toolkit.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Editor.Toolkit
{
    [InitializeOnLoad]
    public static class ToolkitDemoLauncher
    {
        static ToolkitDemoLauncher()
        {
            ToolkitSceneNavigation.editorLoader = LoadEditorScene;
        }

        static bool LoadEditorScene(string scene)
        {
            string path;
            if (scene == "MainToolkit") path = ToolkitUiInstaller.GameplayPath;
            else if (scene == "MenuToolkit") path = ToolkitUiInstaller.MenuPath;
            else return false;
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
        }

        [MenuItem("HealerLike/UI Toolkit/Open Demo")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ToolkitUiValidation.ValidateAssets();
            EditorSceneManager.OpenScene(ToolkitUiInstaller.MenuPath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
            Debug.Log("Toolkit demo opened in the isolated project. Original source and build settings are unchanged.");
        }
    }
}
