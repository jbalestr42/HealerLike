using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Studio.Editor
{
    // The stage pipeline for the length of one preview render, whatever quality preset the editor is on.
    // The look material has a UniversalForwardOnly pass, so the preview has to render through URP.
    public class StudioPipelineScope
    {
        public static readonly string PipelinePath = "Assets/Render/Stage/Settings/StagePipeline.asset";

        RenderPipelineAsset _previous;
        bool _isActive;

        // False, with an error, when the stage pipeline asset is missing; the render then keeps the editor's
        public bool Begin()
        {
            RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError("[StudioPipelineScope] The studio preview needs " + PipelinePath);
                return false;
            }

            _previous = QualitySettings.renderPipeline;
            _isActive = true;
            if (_previous != pipeline)
            {
                QualitySettings.renderPipeline = pipeline;
            }
            return true;
        }

        public void End()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            if (QualitySettings.renderPipeline != _previous)
            {
                QualitySettings.renderPipeline = _previous;
            }
        }
    }
}
