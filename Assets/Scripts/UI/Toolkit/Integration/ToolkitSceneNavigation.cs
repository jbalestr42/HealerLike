using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.UI.Toolkit.Integration
{
    public static class ToolkitSceneNavigation
    {
#if UNITY_EDITOR
        // Installed by our editor adapter; the game's build scene list is never changed.
        public static Func<string, bool> editorLoader;
#endif

        public static bool TryLoad(string scene)
        {
#if UNITY_EDITOR
            if (editorLoader != null && editorLoader(scene))
            {
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
    }
}
