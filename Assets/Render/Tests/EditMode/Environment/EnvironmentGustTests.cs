using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class EnvironmentGustTests
    {
        [Test]
        public void EnvelopeUsesDirectionExpiresAndRejectsInvalidInput()
        {
            GameObject go = new GameObject("GustTest");
            try
            {
                EnvironmentGust gust = go.AddComponent<EnvironmentGust>();

                gust.GustAt(new Vector3(4f, 7f, 0f), 0.8f, 2f, 10);

                Assert.AreEqual(Vector3.zero, gust.Sample(9));
                Assert.AreEqual(Vector3.zero, gust.Sample(double.NaN));
                Assert.That(Vector3.Distance(Vector3.right * 0.8f, gust.Sample(11)), Is.LessThan(0.0001f));
                Assert.AreEqual(Vector3.zero, gust.Sample(12));

                gust.GustAt(Vector3.right, float.NaN, 2f, 12);
                gust.GustAt(Vector3.up, 1f, 2f, 12);
                gust.GustAt(Vector3.right, 1f, -2f, 12);

                Assert.AreEqual(Vector3.zero, gust.Sample(13));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OverlapIsBoundedAndDisableClearsPulsesWithoutAllocation()
        {
            GameObject go = new GameObject("GustTest");
            try
            {
                EnvironmentGust gust = go.AddComponent<EnvironmentGust>();
                for (int i = 0; i < 100; i++)
                {
                    gust.GustAt(Vector3.right, 1f, 2f, 0);
                }

                Assert.AreEqual(Vector3.right, gust.Sample(1));

                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++)
                {
                    gust.GustAt(Vector3.right, 1f, 2f, 0);
                    gust.Sample(1);
                }
                long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, bytes);

                TestHelpers.InvokePrivate(gust, "OnDisable");

                Assert.AreEqual(Vector3.zero, gust.Sample(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
