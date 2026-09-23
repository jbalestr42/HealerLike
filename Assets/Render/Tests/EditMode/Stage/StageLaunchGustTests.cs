using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Environment;

namespace HealerLike.Render.Stage
{

public class StageLaunchGustTests
{
    GameObject _go;
    EnvironmentGust _gust;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("gust");
        _gust = _go.AddComponent<EnvironmentGust>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Launch_SourceToTarget_PushesTheGustAlongIt()
    {
        bool isLaunched = StageLaunchGust.Launch(_gust, new Vector3(1f, 0f, 1f), new Vector3(1f, 3f, 5f));

        Vector3 wind = _gust.Sample(Time.timeAsDouble + StageLaunchGust.Seconds * 0.5f);
        Assert.IsTrue(isLaunched);
        Assert.AreEqual(StageLaunchGust.Strength, wind.z, 0.001f);
        Assert.AreEqual(0f, wind.x, 0.00001f);
        Assert.AreEqual(0f, wind.y);
        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + StageLaunchGust.Seconds + 0.01f).sqrMagnitude);
    }

    [Test]
    public void Launch_NoGust_ReturnsFalse()
    {
        Assert.IsFalse(StageLaunchGust.Launch(null, Vector3.zero, Vector3.forward));
    }

    [Test]
    public void Init_NoProjectileTarget_LeavesTheGustStill()
    {
        StageLaunchGust launch = _go.AddComponent<StageLaunchGust>();
        launch.Init(_gust);

        launch.Init(_go);
        launch.Init((GameObject)null);

        Assert.AreEqual(0f, _gust.Sample(Time.timeAsDouble + 0.1).sqrMagnitude);
    }
}
}
