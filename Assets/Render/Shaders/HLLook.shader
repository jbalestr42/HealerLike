// HL primitive look: flat colour, toon shadow, hatch, outline and banded fog.
Shader "HL/Look/Primitive"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Toggle] _HLNormalEdges ("Normal Edges (zero keeps depth edges only)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "HLLookInput.hlsl"
        #include "HLLookCore.hlsl"

        struct HLAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float3 outlineNormalOS : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct HLVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        HLVaryings HLForwardVertex(HLAttributes input)
        {
            HLVaryings output = (HLVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "HLForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLForwardVertex
            #pragma fragment HLForwardFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 HLForwardFragment(HLVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Screen shadows sample camera UV; atlas/cascades use world-to-shadow.
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1,1,1,1));
                float illum = saturate((dot(normalize(input.normalWS), mainLight.direction) * 0.5 + 0.5) * mainLight.shadowAttenuation);
                return half4(HLEvaluateSurface(input.positionWS, illum, HLGetBaseColor().rgb), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "HLShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLShadowVertex
            #pragma fragment HLShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            HLVaryings HLShadowVertex(HLAttributes input)
            {
                HLVaryings output = HLForwardVertex(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - output.positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(
                    ApplyShadowBias(output.positionWS, output.normalWS, lightDirectionWS)));
                return output;
            }
            half4 HLShadowFragment(HLVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "HLOutline"
            Tags { "LightMode"="HLOutline" }
            Cull Front ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLOutlineVertex
            #pragma fragment HLOutlineFragment
            #pragma multi_compile_instancing
            HLVaryings HLOutlineVertex(HLAttributes input)
            {
                HLVaryings output = HLForwardVertex(input);
                float3 normalOS = dot(input.outlineNormalOS, input.outlineNormalOS) > 1e-12 ? input.outlineNormalOS : input.normalOS;
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionCS = TransformWorldToHClip(HLOutlineExtrude(output.positionWS, normalWS));
                return output;
            }
            half4 HLOutlineFragment(HLVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(HLEvaluateOutline(input.positionWS), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "HLDepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ZTest LEqual ColorMask R
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLDepthVertex
            #pragma fragment HLDepthFragment
            #pragma multi_compile_instancing
            HLVaryings HLDepthVertex(HLAttributes input) { return HLForwardVertex(input); }
            half4 HLDepthFragment(HLVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(input.positionCS.z, 0, 0, 0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "HLDepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Back ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLDepthNormalsVertex
            #pragma fragment HLDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            HLVaryings HLDepthNormalsVertex(HLAttributes input) { return HLForwardVertex(input); }
            half4 HLDepthNormalsFragment(HLVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), HLGetNormalEdges());
                #else
                return half4(normalWS, HLGetNormalEdges());
                #endif
            }
            ENDHLSL
        }
    }
}
