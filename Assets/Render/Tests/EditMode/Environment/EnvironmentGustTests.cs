using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{

public class EnvironmentGustTests
{
    GameObject _go;
    EnvironmentGust _gust;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("GustTest");
        _gust = _go.AddComponent<EnvironmentGust>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Sample_AfterGust_FollowsDirectionThenExpiresAndIgnoresInvalidGusts()
    {
        _gust.GustAt(new Vector3(4f, 7f, 0f), 0.8f, 2f, 10);

        Assert.AreEqual(Vector3.zero, _gust.Sample(9));
        Assert.AreEqual(Vector3.zero, _gust.Sample(double.NaN));
        Assert.That(Vector3.Distance(Vector3.right * 0.8f, _gust.Sample(11)), Is.LessThan(0.0001f));
        Assert.AreEqual(Vector3.zero, _gust.Sample(12));

        _gust.GustAt(Vector3.right, float.NaN, 2f, 12);
        _gust.GustAt(Vector3.up, 1f, 2f, 12);
        _gust.GustAt(Vector3.right, 1f, -2f, 12);

        Assert.AreEqual(Vector3.zero, _gust.Sample(13));
    }

    [Test]
    public void GustAt_ManyOverlapping_StaysBoundedAllocatesNothingAndDisableClears()
    {
        for (int i = 0; i < 100; i++)
        {
            _gust.GustAt(Vector3.right, 1f, 2f, 0);
        }

        Assert.AreEqual(Vector3.right, _gust.Sample(1));

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _gust.GustAt(Vector3.right, 1f, 2f, 0);
            _gust.Sample(1);
        }
        long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, bytes);

        TestHelpers.InvokePrivate(_gust, "OnDisable");

        Assert.AreEqual(Vector3.zero, _gust.Sample(1));
    }
}

}
