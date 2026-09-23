using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{

    public class HLOutlinesPass : ScriptableRenderPass
    {
        readonly int layerMask;
        readonly Material edgeMaterial;
        static readonly ShaderTagId OutlineTag = new ShaderTagId("HLOutline");

        public HLOutlinesPass(int layerMask, Material edgeMaterial)
        {
            this.layerMask = layerMask;
            this.edgeMaterial = edgeMaterial;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            profilingSampler = new ProfilingSampler("HLOutlines");
            if (edgeMaterial != null)
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        class HullData
        {
            public RendererListHandle Renderers;
        }

        class EdgeData
        {
            public Material Material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>();
            var rendering = frameData.Get<UniversalRenderingData>();
            var lights = frameData.Get<UniversalLightData>();
            var drawing = RenderingUtils.CreateDrawingSettings(OutlineTag, rendering, camera, lights, camera.defaultOpaqueSortFlags);
            var filtering = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            var list = renderGraph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
            using (var builder = renderGraph.AddRasterRenderPass<HullData>("HL primitive outlines", out var data, profilingSampler))
            {
                data.Renderers = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc(static (HullData data, RasterGraphContext context) => context.cmd.DrawRendererList(data.Renderers));
            }

            if (edgeMaterial == null || !resources.cameraDepthTexture.IsValid() || !resources.cameraNormalsTexture.IsValid()) return;
            using (var builder = renderGraph.AddRasterRenderPass<EdgeData>("HL depth normal outlines", out var data, profilingSampler))
            {
                data.Material = edgeMaterial;
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.UseTexture(resources.cameraNormalsTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (EdgeData data, RasterGraphContext context) =>
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1));
            }
        }
    }
}
