using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Opens the render stage and authors the render owned assets it runs on. His scenes and data are never written.
    public static class StageMenu
    {
        [MenuItem("Tools/Render/Open Render Stage")]
        public static void OpenStage()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSceneAuthoring.ScenePath) == null)
            {
                Debug.LogError($"[StageMenu] No {StageSceneAuthoring.ScenePath}, run Tools/Render/Author Render Stage first.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(StageSceneAuthoring.ScenePath);
            }
        }

        // Also the batchmode entry point: -executeMethod HealerLike.Render.Stage.StageMenu.AuthorStage
        [MenuItem("Tools/Render/Author Render Stage")]
        public static void AuthorStage()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Object pipeline = StagePipelineAuthoring.Create();
            GameObject environment = EnvironmentAuthoring.Create();
            GameObject controls = StageSceneAuthoring.CreateControls();
            GameObject manager = RenderManagerAuthoring.Create(pipeline, environment, controls);
            StageSceneAuthoring.Create(manager);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageMenu] Authored {StageSceneAuthoring.ScenePath}");
        }
    }
}
