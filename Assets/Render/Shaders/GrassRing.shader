Shader "HL/Grass/HealRing"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "HLGrassHealRing"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLGrassRingVertex
            #pragma fragment HLGrassRingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "GrassZones.hlsl"
            #include "LookCore.hlsl"
            float4 _HL_FieldRect;
            float _HL_SurfaceY;

            struct HLGrassRingInput
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct HLGrassRingOutput
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation float fade : TEXCOORD1;
            };

            HLGrassRingOutput HLGrassRingVertex(HLGrassRingInput input)
            {
                InitIndirectDrawArgs(0);
                uint id = GetIndirectInstanceID(input.instanceID);
                HLGrassRingOutput output = (HLGrassRingOutput)0;
                // Fixed 64-instance draw, no CPU snapshot or GPU readback. Invalid slots are degenerate.
                if (id >= (uint)_HL_ZoneCount)
                {
                    output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
                    return output;
                }

                HLZone zone = HLLoadZone(id);
                if ((zone.kind != 1 && zone.kind != 3) || zone.radius <= 0.0)
                {
                    output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
                    return output;
                }

                float radius = zone.radius + input.uv.y * min(0.018, zone.radius * 0.25);
                float2 p = zone.position.xz + float2(cos(input.uv.x), sin(input.uv.x)) * radius;
                output.positionWS = float3(p.x, _HL_SurfaceY + 0.10, p.y);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fade = HLGrassZoneOnset(zone);
                return output;
            }

            half4 HLGrassRingFragment(HLGrassRingOutput input) : SV_Target
            {
                clip(input.fade - 0.0001);
                clip(input.positionWS.xz - _HL_FieldRect.xy);
                clip(_HL_FieldRect.zw - input.positionWS.xz);
                float hostile = 0.0;
                for (int j = 0; j < min(_HL_ZoneCount, 64); j++)
                {
                    HLZone z = HLLoadZone(j);
                    if (z.kind == 2)
                    {
                        hostile = max(hostile, HLGrassZoneWeight(z, input.positionWS.xz));
                    }
                }
                clip(0.49999 - hostile);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float facing = dot(float3(0.0, 1.0, 0.0), mainLight.direction) * 0.5 + 0.5;
                float illum = saturate(facing * mainLight.shadowAttenuation);
                return half4(HLEvaluateSurface(input.positionWS, illum, float3(1.0, 1.0, 1.0)), input.fade);
            }
            ENDHLSL
        }
    }
}
