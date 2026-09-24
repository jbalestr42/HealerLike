using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public class SpellStudioPreviewPipelineScopeTests
    {
        [Test]
        public void StagePipelineIsScopedAndRestoredAfterException()
        {
            RenderPipelineAsset original=QualitySettings.renderPipeline;
            var low=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Low_PipelineAsset.asset");
            var stage=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(SpellStudioPreviewPipelineScope.PipelinePath);
            Assert.That(low,Is.Not.Null); Assert.That(stage,Is.Not.Null);
            try
            {
                QualitySettings.renderPipeline=low;
                Assert.Throws<InvalidOperationException>(()=>
                {
                    using (new SpellStudioPreviewPipelineScope())
                    {
                        Assert.That(QualitySettings.renderPipeline,Is.SameAs(stage));
                        throw new InvalidOperationException("Test restoration");
                    }
                });
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(low));
                QualitySettings.renderPipeline=null;
                using (new SpellStudioPreviewPipelineScope()) Assert.That(QualitySettings.renderPipeline,Is.SameAs(stage));
                Assert.That(QualitySettings.renderPipeline,Is.Null);
            }
            finally { QualitySettings.renderPipeline=original; }
        }
    }
}
