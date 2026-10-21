using UnityEditor;
using UnityEditor.SceneManagement;

public static class ToolkitDemoLauncher
{
    [MenuItem("Tools/UI Toolkit/Open Demo")]
    public static void Open()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!ToolkitUiValidation.CheckAssets())
        {
            return;
        }

        EditorSceneManager.OpenScene(ToolkitSceneNavigation.MenuPath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying = true;
    }
}
