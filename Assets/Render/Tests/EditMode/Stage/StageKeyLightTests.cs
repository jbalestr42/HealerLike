using HealerLike.Render.Look;
using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Stage
{

public class StageKeyLightTests
{
    GameObject _lightGo;
    Light _light;
    UniversalRenderPipelineAsset _pipeline;
    GameObject _clumpGo;

    [SetUp]
    public void SetUp()
    {
        _lightGo = new GameObject("KeyLightTest");
        _light = _lightGo.AddComponent<Light>();
        _light.type = LightType.Directional;
        _light.shadows = LightShadows.Soft;
        _pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        _clumpGo = new GameObject("ClumpTest");
        _clumpGo.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_lightGo);
        Object.DestroyImmediate(_pipeline);
        Object.DestroyImmediate(_clumpGo);
    }

    [Test]
    public void Aim_KeyDirection_PointsTheLightAlongIt()
    {
        Quaternion rotation = StageKeyLight.Aim(StageKeyLight.KeyDirection);

        Assert.That(Vector3.Angle(-(rotation * Vector3.forward), StageKeyLight.KeyDirection), Is.LessThan(0.01f));
        // Upper right behind the board: the light sits at +x, +z and above the ground
        Assert.That(StageKeyLight.KeyDirection.x, Is.GreaterThan(0));
        Assert.That(StageKeyLight.KeyDirection.y, Is.GreaterThan(0));
        Assert.That(StageKeyLight.KeyDirection.z, Is.GreaterThan(0));
    }

    [Test]
    public void KeyDirection_DefaultToonThreshold_SplitsASphereAboutHalfLitFromThePortraitCamera()
    {
        Vector3 toLight = StageKeyLight.KeyDirection.normalized;
        Vector3 toCamera = Quaternion.Euler(StageCalibration.PortraitPitch, 0f, 0f) * Vector3.back;
        float threshold = LookSettings.Default.toonThreshold;
        float lit = 0f;
        float seen = 0f;

        // Projected area of the visible half, over a latitude and longitude grid
        for (int i = 0; i < 90; i++)
        {
            float latitude = ((i + 0.5f) / 90f - 0.5f) * Mathf.PI;
            for (int j = 0; j < 180; j++)
            {
                float longitude = (j + 0.5f) / 180f * 2f * Mathf.PI;
                Vector3 normal = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude),
                                             Mathf.Cos(latitude) * Mathf.Sin(longitude));
                float area = Vector3.Dot(normal, toCamera) * Mathf.Cos(latitude);
                if (area <= 0f)
                {
                    continue;
                }

                seen += area;
                // Same facing as Look.shader: half Lambert against the toon threshold
                if (Vector3.Dot(normal, toLight) * 0.5f + 0.5f > threshold)
                {
                    lit += area;
                }
            }
        }

        Assert.That(lit / seen, Is.InRange(0.45f, 0.55f));
    }

    [Test]
    public void RendersRealShadows_MissingPipelineShadowsLightOrDistance_ReturnsFalse()
    {
        _pipeline.shadowDistance = 70f;
        bool isSupported = _pipeline.supportsMainLightShadows && _pipeline.mainLightRenderingMode == LightRenderingMode.PerPixel;

        Assert.That(StageKeyLight.RendersRealShadows(_pipeline, _light, 50f), Is.EqualTo(isSupported));
        Assert.That(StageKeyLight.RendersRealShadows(_pipeline, _light, 80f), Is.False, "shadow distance short of the board");

        _light.shadows = LightShadows.None;
        Assert.That(StageKeyLight.RendersRealShadows(_pipeline, _light, 50f), Is.False);

        _light.shadows = LightShadows.Soft;
        _light.type = LightType.Point;
        Assert.That(StageKeyLight.RendersRealShadows(_pipeline, _light, 50f), Is.False);
        Assert.That(StageKeyLight.RendersRealShadows(null, _light, 50f), Is.False);
        Assert.That(StageKeyLight.RendersRealShadows(_pipeline, null, 50f), Is.False);
    }

    [Test]
    public void ApplyCheapShadows_RealShadowsToggled_TurnsEllipsesOffAndBackOn()
    {
        StoneTerrainClump clump = _clumpGo.AddComponent<StoneTerrainClump>();
        Assert.That(clump.groundShadowEnabled, Is.True);

        Assert.That(StageKeyLight.ApplyCheapShadows(true, null, new[] { clump }), Is.EqualTo(1));
        Assert.That(clump.groundShadowEnabled, Is.False);
        Assert.That(StageKeyLight.ApplyCheapShadows(true, null, new[] { clump }), Is.Zero, "already off");

        Assert.That(StageKeyLight.ApplyCheapShadows(false, null, new[] { clump }), Is.Zero);
        Assert.That(clump.groundShadowEnabled, Is.True);
        Assert.DoesNotThrow(() => StageKeyLight.ApplyCheapShadows(true, new StoneEnemyVisual[] { null }, null));
    }
}

}
