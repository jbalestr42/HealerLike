using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Studio.Editor
{

public class StudioPipelineScopeTests
{
    static readonly string lowPipelinePath = "Assets/Settings/Low_PipelineAsset.asset";

    RenderPipelineAsset _original;
    RenderPipelineAsset _stage;

    [SetUp]
    public void SetUp()
    {
        _original = QualitySettings.renderPipeline;
        _stage = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(StudioPipelineScope.PipelinePath);
    }

    [TearDown]
    public void TearDown()
    {
        QualitySettings.renderPipeline = _original;
    }

    [Test]
    public void Begin_EditorOnAnotherPipeline_SwitchesToTheStageAndEndPutsItBack()
    {
        RenderPipelineAsset low = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(lowPipelinePath);
        QualitySettings.renderPipeline = low;
        StudioPipelineScope scope = new StudioPipelineScope();

        Assert.IsTrue(scope.Begin());
        Assert.AreSame(_stage, QualitySettings.renderPipeline);
        scope.End();

        Assert.AreSame(low, QualitySettings.renderPipeline);
    }

    [Test]
    public void End_EditorWithoutPipeline_PutsBackNone()
    {
        QualitySettings.renderPipeline = null;
        StudioPipelineScope scope = new StudioPipelineScope();

        scope.Begin();
        scope.End();
        scope.End(); // a second End changes nothing

        Assert.IsNull(QualitySettings.renderPipeline);
    }
    [Test]
    public void Dispose_FailureInsideScope_RestoresTheOriginalPipeline()
    {
        QualitySettings.renderPipeline = null;
        Assert.Throws<System.InvalidOperationException>(() =>
        {
            using (StudioPipelineScope scope = new StudioPipelineScope())
            {
                Assert.IsTrue(scope.Begin());
                Assert.IsTrue(scope.Begin());
                throw new System.InvalidOperationException("Simulated render failure");
            }
        });

        Assert.IsNull(QualitySettings.renderPipeline);
    }

}

}
