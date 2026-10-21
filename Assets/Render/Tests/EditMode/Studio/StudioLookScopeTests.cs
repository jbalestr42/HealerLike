using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Look;

namespace HealerLike.Render.Studio.Editor
{

public class StudioLookScopeTests
{
    LookShaderProperties.Snapshot _previous;
    GameObject _cameraGo;

    [SetUp]
    public void SetUp()
    {
        _previous = LookShaderProperties.Capture();
        _cameraGo = new GameObject("Studio look camera", typeof(Camera));
    }

    [TearDown]
    public void TearDown()
    {
        _previous.Restore();
        Object.DestroyImmediate(_cameraGo);
    }

    [Test]
    public void Begin_StageGlobals_AppliesTheStudioLook()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        using StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());

        Assert.AreEqual(0.16f, Shader.GetGlobalFloat("_HLInkStrength"));
        Assert.AreEqual(1f, Shader.GetGlobalFloat("_HLLookApplied"));
        Assert.AreEqual(LookSettings.Default.toonThreshold, Shader.GetGlobalFloat("_HLToonThreshold"));
        Assert.AreEqual(LookSettings.Default.toonSoftness, Shader.GetGlobalFloat("_HLToonSoftness"));
        Assert.AreEqual(LookSettings.Default.shadowStrength, Shader.GetGlobalFloat("_HLShadowStrength"));
        scope.End();
    }

    [Test]
    public void End_AfterBegin_PutsBackEveryGlobal()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        Shader.SetGlobalVector("_HLShadowTint", new Vector4(0.1f, 0.2f, 0.3f, 1f));
        using StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.End();

        Assert.AreEqual(0.987f, Shader.GetGlobalFloat("_HLInkStrength"));
        Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.3f, 1f), Shader.GetGlobalVector("_HLShadowTint"));
    }
    [Test]
    public void Dispose_RepeatedBeginAndDispose_RestoresTheOriginalSnapshotOnce()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        using StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.Dispose();
        Shader.SetGlobalFloat("_HLInkStrength", 0.654f);
        scope.Dispose();

        Assert.AreEqual(0.654f, Shader.GetGlobalFloat("_HLInkStrength"));
    }

    [TestCase("_HLOutlineWidthPixels", 1f)]
    [TestCase("_HLFogStart", 500f)]
    [TestCase("_HLFogEnd", 1000f)]
    [TestCase("_HLFogBands", 6f)]
    [TestCase("_HLInkStrength", 0.16f)]
    [TestCase("_HLInkScale", 0.075f)]
    [TestCase("_HLInkWidth", 0.0001f)]
    [TestCase("_HLInkStart", 0.65f)]
    [TestCase("_HLInkRange", 1f)]
    [TestCase("_HLDensityMul", 1f)]
    [TestCase("_HLInkWarp", 0.025f)]
    [TestCase("_HLInkWarpFreq", 2.44f)]
    [TestCase("_HLDashAmount", 0.1f)]
    [TestCase("_HLDashScale", 0.01f)]
    [TestCase("_HLInkDistStart", 100f)]
    [TestCase("_HLInkFarSpacing", 0.6f)]
    [TestCase("_HLContrast", 1f)]
    public void Begin_StudioTheme_KeepsTheAuthoredScalar(string name, float expected)
    {
        using StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());

        Assert.AreEqual(expected, Shader.GetGlobalFloat(name));
    }

    [TestCase("_HLShadowTint", 42f / 255f, 99f / 255f, 137f / 255f)]
    [TestCase("_HLOutlineColor", 0.08f, 0.12f, 0.16f)]
    [TestCase("_HLFogColor", 0.075f, 0.095f, 0.115f)]
    public void Begin_StudioTheme_KeepsTheAuthoredWorkingColor(string name, float red, float green, float blue)
    {
        using StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());

        Color expected = new Color(red, green, blue);
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            expected = expected.linear;
        }
        Assert.AreEqual(new Vector4(expected.r, expected.g, expected.b, 1f), Shader.GetGlobalVector(name));
    }

}

}
