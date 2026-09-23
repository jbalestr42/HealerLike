using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Look;

namespace HealerLike.Render.Stage
{
    // The stage pipeline and renderer, based on his Very High tier plus the outlines and the shadow reach
    public static class StagePipelineAuthoring
    {
        public static readonly string SettingsFolder = "Assets/Render/Stage/Settings";
        public static readonly string PipelinePath = SettingsFolder + "/StagePipeline.asset";
        public static readonly string RendererPath = SettingsFolder + "/StageRenderer.asset";
        public static readonly string EdgeShaderPath = "Assets/Render/Look/HLOutlinesEdges.shader";
        // The board sits 42 to 48 units from the portrait camera
        public static readonly float ShadowDistance = 70f;

        static readonly string sourcePipeline = "Assets/Settings/Very High_PipelineAsset.asset";
        static readonly string sourceRenderer = "Assets/Settings/Very High_PipelineAsset_ForwardRenderer.asset";

        public static UniversalRenderPipelineAsset Create()
        {
            Directory.CreateDirectory(SettingsFolder);
            if (!File.Exists(RendererPath))
            {
                AssetDatabase.CopyAsset(sourceRenderer, RendererPath);
            }

            if (!File.Exists(PipelinePath))
            {
                AssetDatabase.CopyAsset(sourcePipeline, PipelinePath);
            }

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (rendererData == null || pipeline == null)
            {
                Debug.LogError($"[StagePipelineAuthoring] Could not copy {sourcePipeline} and its renderer.");
                return null;
            }

            SetOutlines(rendererData);

            SerializedObject pipelineData = new SerializedObject(pipeline);
            SerializedProperty renderers = pipelineData.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            pipelineData.FindProperty("m_DefaultRendererIndex").intValue = 0;
            SetShadows(pipelineData);
            pipelineData.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        static void SetOutlines(UniversalRendererData rendererData)
        {
            foreach (ScriptableRendererFeature old in rendererData.rendererFeatures.ToArray())
            {
                if (old is Outlines)
                {
                    rendererData.rendererFeatures.Remove(old);
                    Object.DestroyImmediate(old, true);
                }
            }

            Outlines outlines = ScriptableObject.CreateInstance<Outlines>();
            outlines.name = "HLOutlines";
            outlines.edgeShader = AssetDatabase.LoadAssetAtPath<Shader>(EdgeShaderPath);
            if (outlines.edgeShader == null)
            {
                Debug.LogError($"[StagePipelineAuthoring] Missing the edge shader at {EdgeShaderPath}.");
            }

            SetEdges(outlines);
            AssetDatabase.AddObjectToAsset(outlines, rendererData);
            rendererData.rendererFeatures.Add(outlines);
            outlines.SetActive(true);

            // The feature map mirrors the feature list by local file id
            SerializedObject data = new SerializedObject(rendererData);
            SerializedProperty map = data.FindProperty("m_RendererFeatureMap");
            map.arraySize = rendererData.rendererFeatures.Count;
            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererData.rendererFeatures[i], out string _, out long id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            rendererData.SetDirty();
        }

        // Grass writes no normal edge eligibility, so the screen pass does not ink every blade
        public static void SetEdges(Outlines outlines)
        {
            outlines.depthNormalEdges = true;
            outlines.depthThresholdWorld = 1f;
            outlines.referenceDistance = 31f;
            outlines.distanceScale = 1f;
            outlines.normalAngleDegrees = 55f;
            outlines.normalDensityDegrees = 35f;
            outlines.useNormalEdgeMask = true;
        }

        public static void SetShadows(SerializedObject pipeline)
        {
            pipeline.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
            pipeline.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            pipeline.FindProperty("m_ShadowDistance").floatValue = ShadowDistance;
            pipeline.FindProperty("m_ShadowCascadeCount").intValue = 2;
            pipeline.FindProperty("m_Cascade2Split").floatValue = 1f / 3f;
            SerializedProperty map = pipeline.FindProperty("m_MainLightShadowmapResolution");
            map.intValue = Mathf.Max(map.intValue, 2048);
            pipeline.FindProperty("m_SoftShadowsSupported").boolValue = true;
        }
    }
}
