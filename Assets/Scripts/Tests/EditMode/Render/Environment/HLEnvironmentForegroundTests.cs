using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentForegroundTests
    {
        [TestCase(false)] [TestCase(true)] public void CornersFollowCameraAndCountsAreBounded(bool orthographic)
        {
            var go = new GameObject("HLForegroundTest"); var cameraGo = new GameObject("HLCamera");
            try
            {
                var camera = cameraGo.AddComponent<Camera>(); camera.aspect = 9f / 16; camera.orthographic = orthographic;
                var foreground = go.AddComponent<HLEnvironmentForeground>(); foreground.Configure(camera, null, 100);
                Assert.AreEqual(6, foreground.Root.childCount);
                var stone = foreground.Root.GetChild(0); var first = stone.position;
                Vector3 viewport = camera.WorldToViewportPoint(first);
                Assert.That(viewport.x, Is.InRange(-.02f, .06f)); Assert.That(viewport.y, Is.LessThan(.02f));
                Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane));
                camera.transform.position += Vector3.right * 10; foreground.Tick(0);
                Assert.That(stone.position.x - first.x, Is.EqualTo(10).Within(.1f));
                for (int i = 0; i < 10; i++) foreground.Tick(i);
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) foreground.Tick(i);
                long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before; Assert.AreEqual(0, bytes);
                foreground.Configure(camera, null, -1); Assert.AreEqual(4, foreground.Root.childCount);
                foreground.Clear(); Assert.IsNull(foreground.Root);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(cameraGo); }
        }
    }
}
