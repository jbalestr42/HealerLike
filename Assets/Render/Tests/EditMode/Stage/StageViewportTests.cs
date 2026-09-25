using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageViewportTests
    {
        [TestCase(0.5625f, 0.02f, 0.28f, 0.96f, 0.61f)]
        [TestCase(0.462f, 0.02f, 0.24f, 0.96f, 0.64f)]
        [TestCase(1.7778f, 0.22f, 0.3f, 0.62f, 0.56f)]
        [TestCase(1.7778f, 0.55f, 0.15f, 0.4f, 0.72f)]
        public void Fit_KeepsBodiesInsideMeasuredHudSpace(float aspect, float x, float y, float width, float height)
        {
            Bounds bounds = new Bounds(new Vector3(4f, 2f, 7f), new Vector3(18f, 6f, 12f));
            Rect viewport = new Rect(x, y, width, height);
            Rect fit = StageViewport.Inset(viewport, 0.01f);
            Pose pose = StageViewport.Fit(bounds, Quaternion.Euler(52f, 90f, 0f), 40f, aspect, fit);

            Assert.That(StageCalibration.Contains(bounds, pose, 40f, aspect, viewport), Is.True);
        }

        [Test]
        public void Fit_NarrowerUsableArea_MovesCameraBack()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(10f, 4f, 10f));
            Pose wide = StageViewport.Fit(bounds, Quaternion.Euler(52f, 90f, 0f), 40f, 0.5625f,
                new Rect(0.05f, 0.1f, 0.9f, 0.8f));
            Pose narrow = StageViewport.Fit(bounds, wide.rotation, 40f, 0.5625f,
                new Rect(0.05f, 0.35f, 0.9f, 0.3f));
            Assert.That(narrow.position.magnitude, Is.GreaterThan(wide.position.magnitude));
        }

        [TestCase("Main", "Assets/Scenes/Main.unity")]
        [TestCase("MainToolkit", "Assets/Scenes/Main.unity")]
        [TestCase("MenuScene", "Assets/Scenes/Toolkit/MenuToolkit.unity")]
        [TestCase("MenuToolkit", "Assets/Scenes/Toolkit/MenuToolkit.unity")]
        [TestCase("Unrelated", null)]
        public void Navigation_KeepsRenderGameplayAndToolkitMenu(string scene, string expected)
        {
            Assert.That(StageInterface.ScenePath(scene), Is.EqualTo(expected));
        }
    }
}
