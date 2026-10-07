using System.IO;
using NUnit.Framework;
using UnityEditor;

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
    
        [Test]
        public void AndroidVersion_Is0113Code14()
        {
            Assert.That(StagePreviewBuild.AndroidVersion, Is.EqualTo("0.1.13"));
            Assert.That(StagePreviewBuild.AndroidVersionCode, Is.EqualTo(14));
        }

        [Test]
        public void IsAndroidDevelopment_NoReleaseRequest_BuildsADevelopmentPlayer()
        {
            Assert.That(StagePreviewBuild.IsAndroidDevelopment(null), Is.True);
            Assert.That(StagePreviewBuild.IsAndroidDevelopment(""), Is.True);
            Assert.That(StagePreviewBuild.IsAndroidDevelopment("0"), Is.True);
        }

        [Test]
        public void IsAndroidDevelopment_ReleaseRequested_BuildsAReleasePlayer()
        {
            Assert.That(StagePreviewBuild.IsAndroidDevelopment("1"), Is.False);
        }

        [Test]
        public void AndroidOptions_Development_CarriesTheDevelopmentFlag()
        {
            BuildOptions options = StagePreviewBuild.AndroidOptions(true);

            Assert.That(options & BuildOptions.Development, Is.EqualTo(BuildOptions.Development));
        }

        [Test]
        public void AndroidOptions_Release_HasNoDevelopmentFlag()
        {
            Assert.That(StagePreviewBuild.AndroidOptions(false), Is.EqualTo(BuildOptions.None));
        }
    }
}
