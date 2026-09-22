using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentGustTests
    {
        [Test] public void EnvelopeUsesDirectionExpiresAndRejectsInvalidInput()
        {
            var go = new GameObject("HLGustTest");
            try
            {
                var gust = go.AddComponent<HLEnvironmentGust>();
                gust.GustAt(new Vector3(4, 7, 0), .8f, 2, 10);
                Assert.AreEqual(Vector3.zero, gust.Sample(9));
                Assert.AreEqual(Vector3.zero, gust.Sample(double.NaN));
                Assert.That(Vector3.Distance(Vector3.right * .8f, gust.Sample(11)), Is.LessThan(.0001f));
                Assert.AreEqual(Vector3.zero, gust.Sample(12));
                gust.GustAt(Vector3.right, float.NaN, 2, 12);
                gust.GustAt(Vector3.up, 1, 2, 12);
                gust.GustAt(Vector3.right, 1, -2, 12);
                Assert.AreEqual(Vector3.zero, gust.Sample(13));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void OverlapIsBoundedAndDisableClearsPulsesWithoutAllocation()
        {
            var go = new GameObject("HLGustTest");
            try
            {
                var gust = go.AddComponent<HLEnvironmentGust>();
                for (int i = 0; i < 100; i++) gust.GustAt(Vector3.right, 1, 2, 0);
                Assert.AreEqual(Vector3.right, gust.Sample(1));
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) { gust.GustAt(Vector3.right, 1, 2, 0); gust.Sample(1); }
                long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, bytes);
                TestHelpers.InvokePrivate(gust, "OnDisable"); Assert.AreEqual(Vector3.zero, gust.Sample(1));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
