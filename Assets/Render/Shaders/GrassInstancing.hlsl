#ifndef HL_GRASS_INSTANCING_INCLUDED
#define HL_GRASS_INSTANCING_INCLUDED

// Grass blades are the plant cone drawn indirectly: the compute writes one state per blade
// and appends the visible ids, and this path places each cone and gives it one flat colour.

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
float4 _HL_DarkGreen;
float4 _HL_MidGreen;
float4 _HL_LightGreen;
float4 _HL_HealColor;
float4 _HL_SlateColor;
float _HL_BladeHeightScale;

// One flat colour per blade, like a plant part: a green picked by the patch lane,
// leaning toward the heal colour when healed and turning slate as the spike rises
float3 HLGrassColor(HLBladeSeed seed, HLBladeState state)
{
    float patch = seed.heightPhaseWidthRandom.w;
    float3 color = _HL_MidGreen.rgb;
    if (patch < 0.3333)
    {
        color = _HL_DarkGreen.rgb;
    }
    else if (patch >= 0.6667)
    {
        color = _HL_LightGreen.rgb;
    }

    color = lerp(color, _HL_HealColor.rgb, 0.72 * state.rampHealReserved.y);
    return lerp(color, _HL_SlateColor.rgb, saturate(2.0 * state.leanHeightSpike.w));
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
    float heightScale = lerp(_HL_BladeHeightScale, 1.0, spike);
    // Lean scales with height so a shortened blade keeps the silhouette of a spike
    float2 lean = state.leanHeightSpike.xy * heightScale;
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
    blade.color = HLGrassColor(seed, state);
    return blade;
}
#endif

#endif // HL_GRASS_INSTANCING_INCLUDED
