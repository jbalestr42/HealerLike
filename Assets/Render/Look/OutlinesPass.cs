using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{
    // The grass has to write matching depth and normals itself, its indirect draws are not in the hull list
    public class OutlinesPass : ScriptableRenderPass
    {
        class HullData
        {
            public RendererListHandle renderers;
        }

        class EdgeData
        {
            public Material material;
        }

        static readonly ShaderTagId outlineTag = new ShaderTagId("HLOutline");

        readonly int _layerMask;
        readonly Material _edgeMaterial;

        public OutlinesPass(int layerMask, Material edgeMaterial)
        {
            _layerMask = layerMask;
            _edgeMaterial = edgeMaterial;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            profilingSampler = new ProfilingSampler("Outlines");
            if (edgeMaterial != null)
            {
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData camera = frameData.Get<UniversalCameraData>();
            UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>();
            UniversalLightData lights = frameData.Get<UniversalLightData>();
            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(outlineTag, rendering, camera, lights,
                                                                           camera.defaultOpaqueSortFlags);
            FilteringSettings filtering = new FilteringSettings(RenderQueueRange.opaque, _layerMask);
            RendererListParams listParams = new RendererListParams(rendering.cullResults, drawing, filtering);
            RendererListHandle list = renderGraph.CreateRendererList(listParams);
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<HullData>(
                       "Primitive outlines", out HullData data, profilingSampler))
            {
                data.renderers = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc(static (HullData hullData, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(hullData.renderers);
                });
            }

            if (_edgeMaterial == null || !resources.cameraDepthTexture.IsValid()
                || !resources.cameraNormalsTexture.IsValid())
            {
                return;
            }

            // Hardware blending keeps the fill, so no colour copy and no read/write feedback
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<EdgeData>(
                       "Depth normal outlines", out EdgeData data, profilingSampler))
            {
                data.material = _edgeMaterial;
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (EdgeData edgeData, RasterGraphContext context) =>
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, edgeData.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }
        }
    }
}
