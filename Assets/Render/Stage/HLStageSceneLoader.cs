using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // Start-button target of the stage menu copy: loads the stage scene. The stage is not in Julien's Build Settings,
    // so in the Editor it is loaded by path; a player needs the scene added to the build list under SceneName.
    public sealed class HLStageSceneLoader : MonoBehaviour
    {
        public const string SceneName = "HLRenderLook";
        public const string ScenePath = "Assets/Render/Stage/" + SceneName + ".unity";
        // Build index -1 means "not in Build Settings": only the Editor can still load it, by path.
        public static bool UseEditorPath(bool isEditor, int buildIndex) => isEditor && buildIndex < 0;
        public void Load()
        {
#if UNITY_EDITOR
            if (UseEditorPath(Application.isEditor, SceneUtility.GetBuildIndexByScenePath(ScenePath)))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(SceneName);
        }
    }
}
