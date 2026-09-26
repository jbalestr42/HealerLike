using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
public static class ToolkitSceneNavigation
{
    public static readonly string GameplayScene = "MainToolkit";
    public static readonly string MenuScene = "MenuToolkit";
    public static readonly string GameplayPath = "Assets/Scenes/Toolkit/MainToolkit.unity";
    public static readonly string MenuPath = "Assets/Scenes/Toolkit/MenuToolkit.unity";

    public static bool TryLoad(string scene)
    {
#if UNITY_EDITOR
        // The editor loads the demo scenes by path, the game's build scene list is never changed
        string path = GetEditorPath(scene);
        if (path != null)
        {
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
        }
#endif
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            return false;
        }

        SceneManager.LoadScene(scene);
        return true;
    }

#if UNITY_EDITOR
    static string GetEditorPath(string scene)
    {
        if (scene == GameplayScene)
        {
            return GameplayPath;
        }

        if (scene == MenuScene)
        {
            return MenuPath;
        }

        return null;
    }
#endif
}
