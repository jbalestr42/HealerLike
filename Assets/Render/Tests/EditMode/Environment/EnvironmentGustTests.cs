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
    public void Sample_AfterGust_FollowsDirectionThenExpires()
    {
        double start = Time.timeAsDouble;

        _gust.Gust(new Vector3(4f, 7f, 0f), 0.8f, 2f);

        Assert.AreEqual(Vector3.zero, _gust.Sample(start - 1));
        Assert.AreEqual(Vector3.zero, _gust.Sample(double.NaN));
        Assert.That(Vector3.Distance(Vector3.right * 0.8f, _gust.Sample(start + 1)), Is.LessThan(0.0001f));
        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 2.5));
    }

    [Test]
    public void Gust_InvalidPulses_AreIgnored()
    {
        double start = Time.timeAsDouble;

        _gust.Gust(Vector3.right, float.NaN, 2f);
        _gust.Gust(Vector3.up, 1f, 2f);
        _gust.Gust(Vector3.right, 1f, -2f);

        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 1));
    }

    [Test]
    public void Gust_ManyOverlapping_StaysBoundedAllocatesNothingAndDisableClears()
    {
        double start = Time.timeAsDouble;
        for (int i = 0; i < 100; i++)
        {
            _gust.Gust(Vector3.right, 1f, 2f);
        }

        Assert.That(Vector3.Distance(Vector3.right, _gust.Sample(start + 1)), Is.LessThan(0.0001f));

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _gust.Gust(Vector3.right, 1f, 2f);
            _gust.Sample(start + 1);
        }
        long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, bytes);

        TestHelpers.InvokePrivate(_gust, "OnDisable");

        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 1));
    }
}

}
