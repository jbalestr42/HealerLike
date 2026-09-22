Shader "Hidden/HL/Look/DepthNormalOutline"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "HLDepthNormalEdges"
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLEdgeVertex
            #pragma fragment HLEdgeFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "../Shaders/HLLookCore.hlsl"

            struct HLEdgeAttributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct HLEdgeVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            HLEdgeVaryings HLEdgeVertex(HLEdgeAttributes input)
            {
                HLEdgeVaryings output = (HLEdgeVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }
            float HLEyeDepth(float rawDepth)
            {
                // LinearEyeDepth is perspective-only; orthographic depth is already linear.
                #if UNITY_REVERSED_Z
                float ortho = lerp(_ProjectionParams.z, _ProjectionParams.y, rawDepth);
                #else
                float ortho = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                #endif
                return lerp(LinearEyeDepth(rawDepth, _ZBufferParams), ortho, unity_OrthoParams.w);
            }
            float HLEdgeCoverage(float2 uv, float eyeDepth, float3 normalWS)
            {
                float neighborDepth = HLEyeDepth(SampleSceneDepth(uv));
                float3 neighborNormal = SampleSceneNormals(uv);
                // Only the foreground side is inked; sky is left untouched.
                float depthEdge = step(max(.01, eyeDepth * .02), neighborDepth - eyeDepth);
                float normalEdge = step(.2, 1.0 - saturate(dot(normalWS, neighborNormal)));
                normalEdge *= step(.5, dot(normalWS, normalWS)) * step(.5, dot(neighborNormal, neighborNormal));
                return max(depthEdge, normalEdge);
            }
            half4 HLEdgeFragment(HLEdgeVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                if (rawDepth <= 1e-6) return 0;
                #else
                if (rawDepth >= 1.0 - 1e-6) return 0;
                #endif
                float width = max(0.0, HL_G(_HLOutlineWidthPixels, HL_DEF_OUTLINEWIDTHPIXELS));
                float2 delta = width / max(_ScaledScreenParams.xy, 1.0);
                float eyeDepth = HLEyeDepth(rawDepth);
                float3 normalWS = SampleSceneNormals(uv);
                float edge = max(max(HLEdgeCoverage(uv + float2(delta.x, 0), eyeDepth, normalWS),
                                     HLEdgeCoverage(uv - float2(delta.x, 0), eyeDepth, normalWS)),
                                 max(HLEdgeCoverage(uv + float2(0, delta.y), eyeDepth, normalWS),
                                     HLEdgeCoverage(uv - float2(0, delta.y), eyeDepth, normalWS)));
                #if !UNITY_REVERSED_Z
                rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif
                float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                return half4(HLEvaluateOutline(positionWS), edge * saturate(width));
            }
            ENDHLSL
        }
    }
}
