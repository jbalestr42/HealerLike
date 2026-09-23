#ifndef HL_GRASS_INSTANCING_INCLUDED
#define HL_GRASS_INSTANCING_INCLUDED

// Grass blades are the plant cone drawn indirectly: the compute writes one state per blade
// and appends the visible ids, and this path places each cone and colours it per vertex.

// Blades read their own buffers, so procedural instancing needs no per-instance setup
void HLGrassInstancingSetup()
{
}

#if defined(HL_GRASS_INSTANCED)
#define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
#include "UnityIndirect.cginc"

struct HLBladeSeed
{
    float4 positionYaw;
    float4 heightPhaseWidthRandom;
};

struct HLBladeState
{
    float4 leanHeightSpike;
    float4 rampHealReserved;
};

struct HLGrassPlacement
{
    float3 positionWS;
    float3 normalWS;
    float3 color;
};

StructuredBuffer<HLBladeSeed> _HL_BladeSeeds;
StructuredBuffer<HLBladeState> _HL_BladeStates;
StructuredBuffer<uint> _HL_VisibleBladeIDs;
float4 _HL_RootColor;
float4 _HL_MidColor;
float4 _HL_TipColor;
float4 _HL_HealColor;
float4 _HL_SlateRoot;
float4 _HL_SlateTip;
float _HL_BladeHeightScale;

float3 HLGrassRamp(float height01, float heal)
{
    float3 root = lerp(_HL_RootColor.rgb, _HL_HealColor.rgb, 0.22 * heal);
    float3 mid = lerp(_HL_MidColor.rgb, _HL_HealColor.rgb, 0.72 * heal);
    float3 tip = lerp(_HL_TipColor.rgb, _HL_HealColor.rgb, 0.95 * heal);
    if (height01 < 0.45)
    {
        return lerp(root, mid, height01 / 0.45);
    }
    return lerp(mid, tip, (height01 - 0.45) / 0.55);
}

float3 HLGrassColor(HLBladeSeed seed, HLBladeState state, float height01)
{
    float heal = state.rampHealReserved.y;
    // Bruised grass leans part of the way to the spike slate
    float slate = max(state.leanHeightSpike.w, 0.4 * state.rampHealReserved.z);
    float3 slateColor = lerp(_HL_SlateRoot.rgb, _HL_SlateTip.rgb, height01);
    float3 color = lerp(HLGrassRamp(height01, heal), slateColor, saturate(2.0 * slate));
    float3 patchTint = lerp(float3(0.78, 0.96, 1.08), float3(1.08, 1.03, 0.78), seed.heightPhaseWidthRandom.w);
    return color * lerp(patchTint, float3(1.0, 1.0, 1.0), saturate(0.65 * heal + 2.0 * slate));
}

// positionOS and normalOS are the unit cone of HLPrimitiveMeshes: base at y -0.5, apex at y 0.5, radius 0.5
HLGrassPlacement HLPlaceGrassBlade(float3 positionOS, float3 normalOS, uint instanceID)
{
    InitIndirectDrawArgs(0);
    uint bladeID = _HL_VisibleBladeIDs[GetIndirectInstanceID(instanceID)];
    HLBladeSeed seed = _HL_BladeSeeds[bladeID];
    HLBladeState state = _HL_BladeStates[bladeID];

    float spike = step(0.5, state.leanHeightSpike.w);
    float height01 = saturate(positionOS.y + 0.5);
    float2 lean = state.leanHeightSpike.xy;
    float heightScale = lerp(_HL_BladeHeightScale, 1.0, spike);
    float height = max(1e-4, seed.heightPhaseWidthRandom.x * state.leanHeightSpike.z * heightScale);
    float spikeWidth = 2.0 * lerp(0.065, 0.045, saturate(2.0 * state.leanHeightSpike.w - 1.0));
    float width = lerp(seed.heightPhaseWidthRandom.z, spikeWidth, spike);

    float yawCos = cos(seed.positionYaw.w);
    float yawSin = sin(seed.positionYaw.w);
    float3 across = float3(yawCos, 0.0, yawSin);
    float3 along = float3(-yawSin, 0.0, yawCos);
    float3 radial = across * positionOS.x + along * positionOS.z;
    float3 lift = float3(lean.x, height, lean.y) * height01;

    // Inverse transpose of the width and height scale and of the lean shear
    float3 horizontal = (across * normalOS.x + along * normalOS.z) / width;
    float vertical = (normalOS.y - dot(horizontal.xz, lean)) / height;

    HLGrassPlacement blade;
    blade.positionWS = seed.positionYaw.xyz + radial * width + lift;
    blade.normalWS = normalize(float3(horizontal.x, vertical, horizontal.z));
    blade.color = HLGrassColor(seed, state, height01);
    return blade;
}
#endif

#endif // HL_GRASS_INSTANCING_INCLUDED
