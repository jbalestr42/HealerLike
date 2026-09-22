using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentRidgeTests
    {
        [Test] public void FogCalibrationPlacesRootsInLastBand()
        {
            var go = new GameObject("HLRidgeTest"); var cameraGo = new GameObject("HLCamera");
            try
            {
                var camera = cameraGo.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 43, -10);
                var ridge = go.AddComponent<HLEnvironmentRidge>(); ridge.BuildInFogBand(camera, .5f, 44, 51, 6, null);
                Assert.IsNotNull(ridge.Root);
                for (int i = 0; i < ridge.Root.childCount; i++)
                    Assert.That(Vector3.Distance(camera.transform.position, ridge.Root.GetChild(i).position), Is.InRange(51 - 7f / 6, 51));
                ridge.BuildInFogBand(camera, .5f, 1, 2, 6, null); Assert.IsNull(ridge.Root);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(cameraGo); }
        }
        [Test] public void RingIsBoundedDeterministicAndStaysAtAuthoredRadius()
        {
            var go = new GameObject("HLRidgeTest");
            try
            {
                var ridge = go.AddComponent<HLEnvironmentRidge>(); ridge.Build(Vector3.zero, 45, null, 3, 100);
                Assert.AreEqual(HLEnvironmentRidge.MaximumCount, ridge.Root.childCount);
                var first = ridge.Root.GetChild(0).localPosition;
                for (int i = 0; i < ridge.Root.childCount; i++) Assert.That(ridge.Root.GetChild(i).localPosition.magnitude, Is.EqualTo(45).Within(.001f));
                ridge.Build(Vector3.zero, 45, null, 3, 100); Assert.AreEqual(first, ridge.Root.GetChild(0).localPosition);
                ridge.Build(Vector3.zero, float.NaN, null); Assert.IsNull(ridge.Root);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
