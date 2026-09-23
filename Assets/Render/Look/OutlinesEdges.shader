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
            #include "../Shaders/LookCore.hlsl"

            float4 _HLEdgeDepth; // world threshold, reference distance, distance slope
            float4 _HLEdgeNormals; // angle degrees, density penalty degrees, mask enabled

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
            float HLNormalMask(float2 uv)
            {
                return lerp(1.0, SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_PointClamp,
                    UnityStereoTransformScreenSpaceTex(uv)).a, _HLEdgeNormals.z);
            }
            float HLEdgeCoverage(float2 uv, float eyeDepth, float3 normalWS, float mask, float density)
            {
                float neighborDepth = HLEyeDepth(SampleSceneDepth(uv));
                float3 neighborNormal = SampleSceneNormals(uv);
                float threshold = _HLEdgeDepth.x * (1.0 + _HLEdgeDepth.z *
                    max(0.0, eyeDepth / _HLEdgeDepth.y - 1.0));
                // World-unit eye-depth discontinuity; only foreground side inks depth edges.
                float depthEdge = step(threshold, neighborDepth - eyeDepth);
                float angle = min(179.0, _HLEdgeNormals.x + density * _HLEdgeNormals.y);
                float normalEdge = step(1.0 - cos(radians(angle)), 1.0 - clamp(dot(normalWS, neighborNormal), -1.0, 1.0));
                normalEdge *= step(.5, dot(normalWS, normalWS)) * step(.5, dot(neighborNormal, neighborNormal));
                normalEdge *= saturate(mask) * saturate(HLNormalMask(uv));
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
                // Dense alternating normals raise the angle threshold; coherent surfaces retain creases.
                float density = saturate((length(ddx(normalWS)) + length(ddy(normalWS))) * .5);
                float mask = HLNormalMask(uv);
                float edge = max(max(HLEdgeCoverage(uv + float2(delta.x, 0), eyeDepth, normalWS, mask, density),
                                     HLEdgeCoverage(uv - float2(delta.x, 0), eyeDepth, normalWS, mask, density)),
                                 max(HLEdgeCoverage(uv + float2(0, delta.y), eyeDepth, normalWS, mask, density),
                                     HLEdgeCoverage(uv - float2(0, delta.y), eyeDepth, normalWS, mask, density)));
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
