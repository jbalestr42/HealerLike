using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Opens the render stage and authors the render owned assets it runs on. The game's scenes and data are
    // never written.
    public static class StageMenu
    {
        [MenuItem("Tools/Render/Open Render Stage")]
        public static void OpenStage()
        {
            TryOpenStage();
        }

        // GUI/CLI entry point. Leaves the Editor open on the stage with a saved 1080 x 1920 Game view.
        [MenuItem("Tools/Render/Open Portrait Workspace")]
        public static void OpenPortraitWorkspace()
        {
            if (TryOpenStage())
            {
                StageGameViewSize.SelectPersistent(StageCalibration.PortraitWidth, StageCalibration.PortraitHeight);
                Debug.Log("[StageMenu] Portrait workspace ready: RenderStage, 1080 x 1920, healer below the fight.");
            }
        }

        static bool TryOpenStage()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[StageMenu] Stop Play mode before opening the render workspace.");
                return false;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSceneAuthoring.ScenePath) == null)
            {
                Debug.LogError(
                    $"[StageMenu] No {StageSceneAuthoring.ScenePath}, run Tools/Render/Author Render Stage first.");
                return false;
            }

            if (Application.isBatchMode)
            {
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    if (EditorSceneManager.GetSceneAt(i).isDirty)
                    {
                        Debug.LogError("[StageMenu] Open workspace refused: the current scene has unsaved changes.");
                        return false;
                    }
                }
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(StageSceneAuthoring.ScenePath);
            return true;
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
            Debug.Log($"[StageMenu] Authored {StageSceneAuthoring.ScenePath}");
        }
    }
}
