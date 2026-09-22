using UnityEngine;
using UnityEngine.SceneManagement;
namespace HealerLike.Render.Stage
{
    public sealed class HLStageSceneLoader : MonoBehaviour
    {
        public const string SceneName="HLRenderLook";
        public const string ScenePath="Assets/Render/Stage/"+SceneName+".unity";
        public const string MenuName="HLStageMenu";
        public const string MenuPath="Assets/Render/Stage/"+MenuName+".unity";
        public static bool UseEditorPath(bool isEditor,int buildIndex)=>isEditor && buildIndex<0;
        public void Load()=>LoadPath(ScenePath,SceneName);
        public void LoadMenu()=>LoadPath(MenuPath,MenuName);
        static void LoadPath(string path,string name)
        {
            Time.timeScale=1;
#if UNITY_EDITOR
            if(UseEditorPath(Application.isEditor,SceneUtility.GetBuildIndexByScenePath(path)))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(name);
        }
    }
}
