using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Editor.Toolkit
{
    public static class ToolkitDemoLauncher
    {
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
