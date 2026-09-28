using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HealerLike.Render.Stage
{
    // Loads the boot scene next to the render stage, the RenderManager attaches to it once it is loaded
    public class StageLauncher : MonoBehaviour
    {
        // Empty boots StageTarget's default, the Toolkit menu; a path here is an authored override
        [SerializeField] string _scenePath = "";

        // The StageTarget when one was selected, else the authored override, else the menu
        public string scenePath { get { return StageTarget.Resolve(_scenePath); } }

        void Start()
        {
            string path = scenePath;
            LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive);
#if UNITY_EDITOR
            // The game scenes are not in the Build Settings, the editor loads them by path
            EditorSceneManager.LoadSceneInPlayMode(path, parameters);
#else
            SceneManager.LoadScene(path, parameters);
#endif
        }
    }
}
