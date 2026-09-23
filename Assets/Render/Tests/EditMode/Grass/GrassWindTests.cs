using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassWindTests
{
    static GrassWind CreateWind(Vector2 direction, float amplitude)
    {
        GrassWind wind = new GrassWind();
        wind.direction = direction;
        wind.amplitude = amplitude;
        return wind;
    }

    [Test]
    public void Current_NoGust_ReturnsNormalizedDirectionAndAmplitude()
    {
        GrassWind wind = CreateWind(new Vector2(3f, 4f), amplitude: 0.05f);

        Vector4 current = wind.current;

        Assert.AreEqual(0.6f, current.x, 0.0001f);
        Assert.AreEqual(0.8f, current.y, 0.0001f);
        Assert.AreEqual(1.2f, current.z, 0.0001f);
        Assert.AreEqual(0.05f, current.w, 0.0001f);
    }

    [Test]
    public void Current_ZeroDirection_FallsBackToRight()
    {
        GrassWind wind = CreateWind(Vector2.zero, amplitude: 0.05f);

        Assert.AreEqual(new Vector4(1f, 0f, 1.2f, 0.05f), wind.current);
    }

    [Test]
    public void Current_AmplitudeAboveLimit_Clamps()
    {
        GrassWind wind = CreateWind(Vector2.right, amplitude: 3f);

        Assert.AreEqual(GrassWind.MaxAmplitude, wind.current.w);
    }

    [Test]
    public void TriggerGust_TowardTarget_PointsAtTargetAndDoublesAmplitude()
    {
        GrassWind wind = CreateWind(Vector2.right, amplitude: 0.065f);

        wind.TriggerGust(Vector3.forward * 4f);

        Assert.AreEqual(0f, wind.current.x);
        Assert.AreEqual(1f, wind.current.y);
        Assert.AreEqual(0.13f, wind.current.w, 0.0001f); // 0.065 * 2
    }

    [Test]
    public void Advance_PastGustDuration_RestoresBaseline()
    {
        GrassWind wind = CreateWind(Vector2.right, amplitude: 0.065f);
        Vector4 baseline = wind.current;
        wind.TriggerGust(Vector3.forward);

        wind.Advance(0.49f);
        Assert.AreEqual(0.13f, wind.current.w, 0.0001f);

        wind.Advance(0.02f);
        Assert.AreEqual(baseline, wind.current);
    }

    [Test]
    public void TriggerGust_InvalidDirection_IsIgnored()
    {
        GrassWind wind = CreateWind(Vector2.right, amplitude: 0.065f);
        Vector4 baseline = wind.current;

        wind.TriggerGust(new Vector3(float.NaN, 0f, 0f));
        wind.TriggerGust(Vector3.zero);
        wind.TriggerGust(Vector3.up);

        Assert.AreEqual(baseline, wind.current);
    }

    [Test]
    public void ClearGust_DuringGust_RestoresBaseline()
    {
        GrassWind wind = CreateWind(Vector2.right, amplitude: 0.065f);
        Vector4 baseline = wind.current;
        wind.TriggerGust(Vector3.forward);

        wind.ClearGust();

        Assert.AreEqual(baseline, wind.current);
    }
}
}
