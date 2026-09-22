using NUnit.Framework;
namespace HealerLike.Render.Stage
{
    public class HLStageSceneLoaderTests
    {
        [Test] public void EditorLoadsByPathOnlyWhenSceneIsNotInBuildSettings()
        {
            Assert.That(HLStageSceneLoader.UseEditorPath(true, -1), Is.True);
            Assert.That(HLStageSceneLoader.UseEditorPath(true, 0), Is.False);
            Assert.That(HLStageSceneLoader.UseEditorPath(true, 3), Is.False);
        }
        [Test] public void PlayerAlwaysLoadsByName()
        {
            Assert.That(HLStageSceneLoader.UseEditorPath(false, -1), Is.False);
            Assert.That(HLStageSceneLoader.UseEditorPath(false, 2), Is.False);
        }
        [Test] public void SceneNameMatchesScenePath()
        {
            Assert.That(System.IO.Path.GetFileNameWithoutExtension(HLStageSceneLoader.ScenePath), Is.EqualTo(HLStageSceneLoader.SceneName));
            Assert.That(HLStageSceneLoader.ScenePath, Does.StartWith("Assets/Render/Stage/"));
        }
    }
}
