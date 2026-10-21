using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreaturePortraitRendererTests
    {
        GameObject _host;
        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("portrait frame test");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void OwnedPortraitCameraClearsTransparentWithoutPostProcessing()
        {
            using (CreaturePortraitRenderer renderer = new CreaturePortraitRenderer(null, null))
            {
                TestHelpers.InvokePrivate(renderer, "InitCamera");
                var field = typeof(CreaturePortraitRenderer).GetField("_camera",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Camera camera = (Camera)field.GetValue(renderer);
                Assert.That(camera.backgroundColor, Is.EqualTo(Color.clear));
                Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
                Assert.That(camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()
                    .renderPostProcessing, Is.False);
            }
        }

        [TestCase(4f, 1f, 2f)]
        [TestCase(1f, 6f, 1f)]
        [TestCase(0.1f, 0.1f, 0.1f)]
        public void Frame_KeepsEveryBoundsCornerInsideTheImageWithInset(float width, float height, float depth)
        {
            Camera camera = _host.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            Bounds bounds = new Bounds(new Vector3(0f, -4096f, 0f), new Vector3(width, height, depth));
            CreaturePortraitRenderer.Frame(camera, bounds);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 viewport = camera.WorldToViewportPoint(corner);
                        Assert.That(viewport.x, Is.InRange(0.08f, 0.92f));
                        Assert.That(viewport.y, Is.InRange(0.08f, 0.92f));
                        Assert.That(viewport.z, Is.InRange(camera.nearClipPlane, camera.farClipPlane));
                    }
                }
            }
        }
    }
}
