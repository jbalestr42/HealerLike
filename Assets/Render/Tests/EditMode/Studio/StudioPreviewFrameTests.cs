using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Studio.Editor
{

public class StudioPreviewFrameTests
{
    PreviewRenderUtility _utility;
    RenderPipelineAsset _pipeline;
    RenderTexture _active;
    float _ink;
    Texture2D _image;

    [SetUp]
    public void SetUp()
    {
        _utility = new PreviewRenderUtility();
        _pipeline = QualitySettings.renderPipeline;
        _active = RenderTexture.active;
        _ink = Shader.GetGlobalFloat("_HLInkStrength");
    }

    [TearDown]
    public void TearDown()
    {
        if (_image != null)
        {
            UnityEngine.Object.DestroyImmediate(_image);
        }
        _utility.Cleanup();
        QualitySettings.renderPipeline = _pipeline;
        RenderTexture.active = _active;
        Shader.SetGlobalFloat("_HLInkStrength", _ink);
    }

    [Test]
    public void Capture_RenderFailure_ClosesPreviewAndRestoresBorrowedState()
    {
        Shader.SetGlobalFloat("_HLInkStrength", 0.987f);
        Assert.Throws<InvalidOperationException>(() =>
            StudioPreviewFrame.Capture(_utility, 32, 32, Fail));

        Assert.AreSame(_pipeline, QualitySettings.renderPipeline);
        Assert.AreSame(_active, RenderTexture.active);
        Assert.AreEqual(0.987f, Shader.GetGlobalFloat("_HLInkStrength"));
        _image = StudioPreviewFrame.Capture(_utility, 32, 32, () => { });
        Assert.IsNotNull(_image);
        Assert.AreEqual(32, _image.width);
    }

    static void Fail()
    {
        throw new InvalidOperationException("Simulated render failure");
    }
}

}
