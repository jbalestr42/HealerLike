using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Look;

namespace HealerLike.Render.Studio.Editor
{

public class StudioLookScopeTests
{
    float _ink;
    float _applied;
    Vector4 _tint;
    GameObject _cameraGo;

    [SetUp]
    public void SetUp()
    {
        _ink = Shader.GetGlobalFloat("_HLInkStrength");
        _applied = Shader.GetGlobalFloat("_HLLookApplied");
        _tint = Shader.GetGlobalVector("_HLShadowTint");
        _cameraGo = new GameObject("Studio look camera", typeof(Camera));
    }

    [TearDown]
    public void TearDown()
    {
        Shader.SetGlobalFloat("_HLInkStrength", _ink);
        Shader.SetGlobalFloat("_HLLookApplied", _applied);
        Shader.SetGlobalVector("_HLShadowTint", _tint);
        Object.DestroyImmediate(_cameraGo);
    }

    [Test]
    public void Begin_StageGlobals_AppliesTheStudioLook()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        StudioLookScope scope = new StudioLookScope();

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
        StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.End();

        Assert.AreEqual(0.987f, Shader.GetGlobalFloat("_HLInkStrength"));
        Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.3f, 1f), Shader.GetGlobalVector("_HLShadowTint"));
    }
    [Test]
    public void Dispose_RepeatedBeginAndDispose_RestoresTheOriginalSnapshotOnce()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        StudioLookScope scope = new StudioLookScope();

        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.Begin(_cameraGo.GetComponent<Camera>());
        scope.Dispose();
        Shader.SetGlobalFloat("_HLInkStrength", 0.654f);
        scope.Dispose();

        Assert.AreEqual(0.654f, Shader.GetGlobalFloat("_HLInkStrength"));
    }

}

}
