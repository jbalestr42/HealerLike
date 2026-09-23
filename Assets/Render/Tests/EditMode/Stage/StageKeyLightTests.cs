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
    GameObject _discGo;

    [SetUp]
    public void SetUp()
    {
        _lightGo = new GameObject("KeyLightTest");
        _light = _lightGo.AddComponent<Light>();
        _light.type = LightType.Directional;
        _light.shadows = LightShadows.Soft;
        _pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        _discGo = new GameObject("DiscTest");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_lightGo);
        Object.DestroyImmediate(_pipeline);
        Object.DestroyImmediate(_discGo);
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
    public void KeyDirection_PortraitCamera_CastsShadowsToTheLowerLeft()
    {
        Quaternion camera = Quaternion.Euler(StageCalibration.PortraitPitch, 0f, 0f);
        Vector3 toLight = StageKeyLight.KeyDirection.normalized;
        // Where a point above the ground lands along the light, seen in the camera's axes
        Vector3 shadow = Quaternion.Inverse(camera) * new Vector3(-toLight.x, 0f, -toLight.z);

        Assert.That(shadow.x, Is.LessThan(0f));
        Assert.That(shadow.y, Is.LessThan(0f));
        Assert.That(Mathf.Atan2(-shadow.y, -shadow.x) * Mathf.Rad2Deg, Is.InRange(10f, 45f)); // below the horizontal
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
        StoneGroundDisc shadow = StoneGroundDiscTests.CreateDisc(_discGo.transform, true);
        StoneGroundDisc earth = StoneGroundDiscTests.CreateDisc(_discGo.transform, false);
        shadow.Show(true);
        earth.Show(true);
        StoneGroundDisc[] discs = new StoneGroundDisc[] { shadow, earth };

        Assert.That(StageKeyLight.ApplyCheapShadows(true, discs), Is.EqualTo(1));
        Assert.That(shadow.gameObject.activeSelf, Is.False);
        Assert.That(earth.gameObject.activeSelf, Is.True, "the bare earth is not a shadow");
        Assert.That(StageKeyLight.ApplyCheapShadows(true, discs), Is.Zero, "already off");

        Assert.That(StageKeyLight.ApplyCheapShadows(false, discs), Is.Zero);
        Assert.That(shadow.gameObject.activeSelf, Is.True);
        Assert.DoesNotThrow(() => StageKeyLight.ApplyCheapShadows(true, new StoneGroundDisc[] { null }));
        Assert.DoesNotThrow(() => StageKeyLight.ApplyCheapShadows(true, null));
    }
}

}
