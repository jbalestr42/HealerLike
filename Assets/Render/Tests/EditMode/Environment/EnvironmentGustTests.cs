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
    public void Sample_OnThePath_FollowsItsDirectionThenExpires()
    {
        double start = Time.timeAsDouble;

        _gust.Gust(Vector3.zero, new Vector3(4f, 7f, 0f), 0.8f, 2f);

        Assert.AreEqual(Vector3.zero, _gust.Sample(start - 1, Vector3.right));
        Assert.AreEqual(Vector3.zero, _gust.Sample(double.NaN, Vector3.right));
        Assert.That(Vector3.Distance(Vector3.right * 0.8f, _gust.Sample(start + 1, new Vector3(2f, 0f, 0.2f))),
            Is.LessThan(0.0001f));
        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 2.5, Vector3.right));
    }

    [Test]
    public void Sample_AwayFromThePath_FadesToNothing()
    {
        double start = Time.timeAsDouble;

        _gust.Gust(Vector3.zero, new Vector3(4f, 0f, 0f), 1f, 2f);

        float near = _gust.Sample(start + 1, new Vector3(2f, 0f, EnvironmentGust.Near)).magnitude;
        float half = _gust.Sample(start + 1, new Vector3(2f, 0f, 0.5f * (EnvironmentGust.Near + EnvironmentGust.Reach))).magnitude;
        float far = _gust.Sample(start + 1, new Vector3(2f, 0f, EnvironmentGust.Reach + 1f)).magnitude;
        float behind = _gust.Sample(start + 1, new Vector3(-3f, 0f, 0f)).magnitude;

        Assert.AreEqual(1f, near, 1e-4f);
        Assert.AreEqual(0.5f, half, 1e-4f);
        Assert.AreEqual(0f, far, "A plant far from the shot stays still.");
        Assert.AreEqual(0f, behind, "Nor one well behind the shooter.");
    }

    [Test]
    public void Gust_InvalidPulses_AreIgnored()
    {
        double start = Time.timeAsDouble;

        _gust.Gust(Vector3.zero, Vector3.right, float.NaN, 2f);
        _gust.Gust(Vector3.zero, Vector3.up, 1f, 2f);
        _gust.Gust(Vector3.zero, Vector3.right, 1f, -2f);
        _gust.Gust(new Vector3(float.NaN, 0f, 0f), Vector3.right, 1f, 2f);

        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 1, Vector3.right * 0.5f));
    }

    [Test]
    public void Gust_ManyOverlapping_StaysBoundedAllocatesNothingAndDisableClears()
    {
        double start = Time.timeAsDouble;
        for (int i = 0; i < 100; i++)
        {
            _gust.Gust(Vector3.zero, Vector3.right, 1f, 2f);
        }

        Assert.That(Vector3.Distance(Vector3.right, _gust.Sample(start + 1, Vector3.right * 0.5f)),
            Is.LessThan(0.0001f));

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _gust.Gust(Vector3.zero, Vector3.right, 1f, 2f);
            _gust.Sample(start + 1, Vector3.right * 0.5f);
        }
        long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, bytes);

        TestHelpers.InvokePrivate(_gust, "OnDisable");

        Assert.AreEqual(Vector3.zero, _gust.Sample(start + 1, Vector3.right * 0.5f));
    }
}

}
