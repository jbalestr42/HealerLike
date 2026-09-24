using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    /// <summary>Preview the renderer with its production pipeline, independently of the editor's quality preset.</summary>
    public sealed class SpellStudioPreviewPipelineScope : IDisposable
    {
        public const string PipelinePath = "Assets/Render/Stage/Settings/StagePipeline.asset";
        readonly RenderPipelineAsset previous;
        bool disposed;

        public SpellStudioPreviewPipelineScope()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (pipeline == null) throw new InvalidOperationException("Render Studio requires " + PipelinePath);
            previous = QualitySettings.renderPipeline;
            if (previous != pipeline) QualitySettings.renderPipeline = pipeline;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (QualitySettings.renderPipeline != previous) QualitySettings.renderPipeline = previous;
        }
    }
}
