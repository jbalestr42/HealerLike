// The ground, run by GroundSimulation off-screen. Motion: stamps add up into a held target and a kicked force, then
// fixed steps move a damped, neighbour-coupled spring of lean toward the target under the force and ease a
// flatness toward the stamped one. State: auras add up into what they ask for, and once a frame the ash,
// vitality and glow ease toward it.
// Texel (x, y) always holds uv ((x + 0.5) / width, (y + 0.5) / height), whatever the platform's row order.
Shader "Hidden/HL/GroundSimulation"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "GroundCommon.hlsl"

        float4 _HLGroundRect;
        // xy texels, zw their reciprocal
        float4 _HLGroundSize;

        float2 HLTexelUV(float4 positionCS)
        {
            return positionCS.xy * _HLGroundSize.zw;
        }

        // A triangle covering the target from three vertex ids
        float4 HLFullScreen(uint vertexID : SV_VertexID) : SV_POSITION
        {
            float2 corner = float2((vertexID << 1) & 2, vertexID & 2);
            return float4(corner * 2.0 - 1.0, 0.5, 1.0);
        }

        // The texel's row as the platform stores it: row 0 is uv 0 on every platform once the clip-space y
        // follows the texture's row order
        float4 HLGroundClip(float2 uv)
        {
            float4 positionCS = float4(uv * 2.0 - 1.0, 0.5, 1.0);
            #if UNITY_UV_STARTS_AT_TOP
            positionCS.y = -positionCS.y;
            #endif
            return positionCS;
        }

        StructuredBuffer<HLGroundStamp> _HLGroundStamps;

        struct HLStampVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 positionXZ : TEXCOORD0;
            nointerpolation uint stampID : TEXCOORD1;
        };

        HLStampVaryings HLStampVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
        {
            HLGroundStamp stamp = _HLGroundStamps[instanceID];
            // Two triangles over the square the stamp can reach, two texels wider so its edge is never cut
            const float2 corners[6] =
            {
                float2(-1.0, -1.0), float2(1.0, -1.0), float2(1.0, 1.0),
                float2(-1.0, -1.0), float2(1.0, 1.0), float2(-1.0, 1.0)
            };
            float2 side = corners[vertexID % 6];
            float2 centre;
            float reach;
            HLGroundStampBounds(stamp, centre, reach);
            reach += 2.0 * max(_HLGroundSize.z / _HLGroundRect.z, _HLGroundSize.w / _HLGroundRect.w);
            HLStampVaryings output;
            output.positionXZ = centre + side * reach;
            output.positionCS = HLGroundClip(HLGroundUV(output.positionXZ, _HLGroundRect));
            output.stampID = instanceID;
            return output;
        }
        ENDHLSL

        // 0: every stamp, one quad each, adding its held lean and flatness into the target
        Pass
        {
            Name "HLGroundStamp"
            Blend One One
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLStampVertex
            #pragma fragment HLStampFragment

            float4 HLStampFragment(HLStampVaryings input) : SV_Target
            {
                HLGroundStamp stamp = _HLGroundStamps[input.stampID];
                float3 value = HLGroundStampValue(stamp, input.positionXZ);
                return float4(value.xy * stamp.response.z, value.z, 0.0);
            }
            ENDHLSL
        }

        // 1: one spring step of the lean, reading the previous state and the target
        Pass
        {
            Name "HLGroundLean"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLFullScreen
            #pragma fragment HLLeanFragment

            Texture2D<float4> _HLGroundPrevious;
            Texture2D<float4> _HLGroundTarget;
            Texture2D<float4> _HLGroundForce;
            float4 _HLGroundSpring;
            float4 _HLGroundWind;
            float _HLGroundStep;

            float4 HLLoadState(int2 texel)
            {
                int2 size = int2(_HLGroundSize.xy);
                return _HLGroundPrevious.Load(int3(clamp(texel, int2(0, 0), size - 1), 0));
            }

            float4 HLLeanFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                int2 texel = int2(positionCS.xy);
                float4 state = HLLoadState(texel);
                float2 neighbourMean = 0.25 * (HLLoadState(texel + int2(1, 0)).xy + HLLoadState(texel - int2(1, 0)).xy
                    + HLLoadState(texel + int2(0, 1)).xy + HLLoadState(texel - int2(0, 1)).xy);
                float2 p = _HLGroundRect.xy + HLTexelUV(positionCS) / _HLGroundRect.zw;
                float2 stamped = _HLGroundTarget.Load(int3(texel, 0)).xy;
                float2 target = HLGroundCapLean(stamped + HLGroundWindLean(p, _HLGroundWind),
                                                _HLGroundSpring.w);
                float2 force = _HLGroundForce.Load(int3(texel, 0)).xy;
                return HLGroundSpringStep(state, target, neighbourMean, force, _HLGroundStep, _HLGroundSpring);
            }
            ENDHLSL
        }

        // 2: one step of the flatness toward the stamped one
        Pass
        {
            Name "HLGroundCrush"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLFullScreen
            #pragma fragment HLCrushFragment

            Texture2D<float> _HLGroundPreviousCrush;
            Texture2D<float4> _HLGroundTarget;
            float2 _HLGroundCrushRates;
            float _HLGroundStep;

            float HLCrushFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                int2 texel = int2(positionCS.xy);
                float crush = _HLGroundPreviousCrush.Load(int3(texel, 0));
                float target = saturate(_HLGroundTarget.Load(int3(texel, 0)).z);
                return HLGroundCrushStep(crush, target, _HLGroundStep, _HLGroundCrushRates);
            }
            ENDHLSL
        }

        // 3: every stamp again, adding its kick into the force
        Pass
        {
            Name "HLGroundForce"
            Blend One One
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLStampVertex
            #pragma fragment HLForceFragment

            float4 HLForceFragment(HLStampVaryings input) : SV_Target
            {
                HLGroundStamp stamp = _HLGroundStamps[input.stampID];
                float3 value = HLGroundStampValue(stamp, input.positionXZ);
                return float4(value.xy * stamp.response.w, 0.0, 0.0);
            }
            ENDHLSL
        }

        // 4: every stamp again, adding what the auras ask of the state
        Pass
        {
            Name "HLGroundAura"
            Blend One One
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLStampVertex
            #pragma fragment HLAuraFragment

            float4 HLAuraFragment(HLStampVaryings input) : SV_Target
            {
                return HLGroundStampState(_HLGroundStamps[input.stampID], input.positionXZ);
            }
            ENDHLSL
        }

        // 5: one frame of the state toward what the auras ask
        Pass
        {
            Name "HLGroundState"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex HLFullScreen
            #pragma fragment HLStateFragment

            Texture2D<float4> _HLGroundPreviousState;
            Texture2D<float4> _HLGroundAura;
            float4 _HLGroundStateRates;
            float2 _HLGroundGlowRates;
            float _HLGroundStep;

            float4 HLStateFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                int3 texel = int3(int2(positionCS.xy), 0);
                return HLGroundStateStep(_HLGroundPreviousState.Load(texel), _HLGroundAura.Load(texel),
                                         _HLGroundStep, _HLGroundStateRates, _HLGroundGlowRates);
            }
            ENDHLSL
        }
    }
}
