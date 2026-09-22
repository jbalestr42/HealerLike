#ifndef HL_GRASS_DATA_INCLUDED
#define HL_GRASS_DATA_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
#include "UnityIndirect.cginc"
#include "HLLookCore.hlsl"
struct HLBladeSeed { float4 positionYaw; float4 heightPhaseWidthRandom; };
struct HLBladeState { float4 leanHeightSpike; float4 rampHealReserved; };
StructuredBuffer<HLBladeSeed> _HL_BladeSeeds;
StructuredBuffer<HLBladeState> _HL_BladeStates;
StructuredBuffer<uint> _HL_VisibleBladeIDs;
CBUFFER_START(UnityPerMaterial)
float4 _HL_RootColor, _HL_MidColor, _HL_TipColor, _HL_HealColor, _HL_SlateRoot, _HL_SlateTip;
float _HL_Cone;
float _HL_Cull;
float4 _HL_InstanceTint;
CBUFFER_END
UNITY_INSTANCING_BUFFER_START(HLGrassInstances)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_INSTANCING_BUFFER_END(HLGrassInstances)
void HLGrassSetup()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    _BaseColor = _HL_InstanceTint;
#endif
}
struct HLGrassAttributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    uint instanceID : SV_InstanceID;
};
struct HLGrassVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float height01 : TEXCOORD2;
    nointerpolation float2 healSpike : TEXCOORD3;
    nointerpolation float patchHue : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
HLGrassVaryings HLGrassVertex(HLGrassAttributes input)
{
    HLGrassVaryings output = (HLGrassVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    InitIndirectDrawArgs(0);
    uint id = _HL_VisibleBladeIDs[GetIndirectInstanceID(input.instanceID)];
    HLBladeSeed seed = _HL_BladeSeeds[id];
    HLBladeState state = _HL_BladeStates[id];
    float t = input.uv.y;
    float2 lean = state.leanHeightSpike.xy;
    float height = seed.heightPhaseWidthRandom.x * state.leanHeightSpike.z;
    float c = cos(seed.positionYaw.w), s = sin(seed.positionYaw.w);
    float3 B = float3(c, 0, s), Z = float3(-s, 0, c);
    float3 local, normal;
    if (_HL_Cone > 0.5)
    {
        float radius = lerp(0.065, 0.045, saturate(2 * state.leanHeightSpike.w - 1));
        local = B * (input.positionOS.x * radius) + Z * (input.positionOS.z * radius)
            + float3(lean.x * t, height * t, lean.y * t);
        // Inverse transpose of radius scaling plus linear tip shear; preserves flat faces.
        float3 horizontal = B * input.normalOS.x + Z * input.normalOS.z;
        normal = normalize(float3(horizontal.x / radius,
            (input.normalOS.y - dot(horizontal.xz, lean) / radius) / height, horizontal.z / radius));
    }
    else
    {
        local = B * (input.positionOS.x * seed.heightPhaseWidthRandom.z)
            + float3(lean.x * t * t, height * t, lean.y * t * t);
        normal = normalize(cross(B, float3(2 * lean.x * t, height, 2 * lean.y * t)));
    }
    output.positionWS = seed.positionYaw.xyz + local;
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = normal;
    output.height01 = t;
    output.patchHue = seed.heightPhaseWidthRandom.w;
    output.healSpike = float2(state.rampHealReserved.y, max(state.leanHeightSpike.w, 0.4 * state.rampHealReserved.z));
    return output;
}
float3 HLGrassAlbedo(float t, float2 healSpike)
{
    float3 root = lerp(_HL_RootColor.rgb, _HL_HealColor.rgb, 0.22 * healSpike.x);
    float3 mid = lerp(_HL_MidColor.rgb, _HL_HealColor.rgb, 0.72 * healSpike.x);
    float3 tip = lerp(_HL_TipColor.rgb, _HL_HealColor.rgb, 0.95 * healSpike.x);
    float3 grass = t < 0.45 ? lerp(root, mid, t / 0.45) : lerp(mid, tip, (t - 0.45) / 0.55);
    return lerp(grass, lerp(_HL_SlateRoot.rgb, _HL_SlateTip.rgb, t), saturate(2 * healSpike.y));
}
half4 HLGrassFragment(HLGrassVaryings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float3 normal = normalize(input.normalWS) * (float(face) > 0 ? 1 : -1);
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
#else
    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    Light mainLight = GetMainLight(shadowCoord);
    // Rounded clump shading: upper blade normals turn toward the sky to catch the key.
    // The resulting illumination still enters the shared threshold/tint/ramp only once.
    if (input.healSpike.y < 0.5) normal = normalize(normal + float3(0, input.height01 * input.height01 * 1.4, 0));
    float illum = saturate((dot(normal, mainLight.direction) * 0.5 + 0.5) * mainLight.shadowAttenuation);
    float3 baseColor = HLGrassAlbedo(input.height01, input.healSpike) * UNITY_ACCESS_INSTANCED_PROP(HLGrassInstances, _BaseColor).rgb;
    float3 patchTint = lerp(float3(0.78, 0.96, 1.08), float3(1.08, 1.03, 0.78), input.patchHue);
    baseColor *= lerp(patchTint, float3(1,1,1), saturate(0.65 * input.healSpike.x + 2 * input.healSpike.y));
    // Look beauty tip light: lit blade tips lift slightly; shadow and roots unchanged. _HLTipLight 0 disables it.
    float3 color = HLShadeSurface(input.positionWS, illum, baseColor);
    color = HLApplyTipLight(color, input.height01, illum);
    return half4(HLApplyBandedFog(input.positionWS, color), 1);
}
half4 HLGrassDepth(HLGrassVaryings input) : SV_Target { return 0; }
half4 HLGrassNormals(HLGrassVaryings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
{
    float3 normal = normalize(input.normalWS) * (float(face) > 0 ? 1 : -1);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normal);
    return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
#else
    return half4(normal, 0);
#endif
}
#endif
