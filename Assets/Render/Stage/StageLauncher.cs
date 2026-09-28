using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HealerLike.Render.Stage
{
    // Loads the game scene next to the render stage, the RenderManager attaches to it once it is loaded
    public class StageLauncher : MonoBehaviour
    {
        [SerializeField] string _scenePath = "Assets/Scenes/Main.unity";

        // The StageTarget when the menu selected one, the authored scene otherwise
        public string scenePath { get { return StageTarget.Resolve(_scenePath); } }

        void Start()
        {
            string path = scenePath;
            LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive);
#if UNITY_EDITOR
            // Main is not in the Build Settings, the editor loads it by path
            EditorSceneManager.LoadSceneInPlayMode(path, parameters);
#else
            SceneManager.LoadScene(path, parameters);
#endif
        }
    }
}
