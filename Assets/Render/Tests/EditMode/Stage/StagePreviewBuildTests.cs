using System.IO;
using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class StagePreviewBuildTests
    {
        [Test]
        public void Scenes_ShipTheSandboxBesideTheExistingRoute()
        {
            CollectionAssert.Contains(StagePreviewBuild.Scenes, "Assets/Scenes/Sandbox.unity");
            CollectionAssert.Contains(StagePreviewBuild.Scenes, StageTarget.SandboxPath);
            CollectionAssert.Contains(StagePreviewBuild.Scenes, StageTarget.MainPath);
            CollectionAssert.Contains(StagePreviewBuild.Scenes, StageInterface.MenuPath);
        }

        [Test]
        public void Scenes_StartOnTheRenderStage()
        {
            Assert.That(StagePreviewBuild.Scenes[0], Is.EqualTo(StageSceneAuthoring.ScenePath));
        }

        [Test]
        public void Scenes_AreUniqueAndExistOnDisk()
        {
            CollectionAssert.AllItemsAreUnique(StagePreviewBuild.Scenes);
            foreach (string scene in StagePreviewBuild.Scenes)
            {
                Assert.That(File.Exists(scene), Is.True, scene);
            }
        }
    }
}
