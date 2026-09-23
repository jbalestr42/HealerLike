// Capture-only analytic grass-tip adapter; no texture is sampled.
Shader "Hidden/HL/Look/BeautyProbe"
{
    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Shaders/LookCore.hlsl"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input i) { Output o; o.vertex = TransformObjectToHClip(i.vertex.xyz); o.uv = i.uv; return o; }
            half4 frag(Output i) : SV_Target
            {
                return half4(HLApplyTipLight(HLWorkingColor(float3(.3,.65,.3)), i.uv.y, step(.5,i.uv.x)),1);
            }
            ENDHLSL
        }
    }
}
