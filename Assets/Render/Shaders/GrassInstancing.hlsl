#ifndef HL_GRASS_INSTANCING_INCLUDED
#define HL_GRASS_INSTANCING_INCLUDED

// Grass is the solid tuft mesh drawn indirectly: the compute writes one state per tuft
// and appends the visible ids, and this path places each tuft rigidly and gives it one flat colour.

// Tufts read their own buffers, so procedural instancing needs no per-instance setup
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

// Rotates v about the horizontal axis that tips +Y toward lean, by the length of lean in radians
float3 HLTiltGrassTuft(float3 v, float2 lean)
{
    float angle = max(length(lean), 1e-5);
    float3 axis = float3(lean.y, 0.0, -lean.x) / angle;
    float c = cos(angle);
    float s = sin(angle);
    return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
}

// Yaw in radians about +Y, the turn Quaternion.Euler(0, yaw, 0) makes
float3 HLYawGrassTuft(float3 v, float yaw)
{
    float c = cos(yaw);
    float s = sin(yaw);
    return float3(v.x * c + v.z * s, v.y, v.z * c - v.x * s);
}

// positionOS and normalOS are the unit tuft of GrassTuft: base on y 0, main tip at y 1, main width 1.
// The tuft moves as a rigid body: scale, yaw, one tilt about its root, then the root position.
// GrassTuft.Place and PlaceNormal mirror this on the CPU.
HLGrassPlacement HLPlaceGrassBlade(float3 positionOS, float3 normalOS, uint instanceID)
{
    InitIndirectDrawArgs(0);
    uint bladeID = _HL_VisibleBladeIDs[GetIndirectInstanceID(instanceID)];
    HLBladeSeed seed = _HL_BladeSeeds[bladeID];
    HLBladeState state = _HL_BladeStates[bladeID];

    float spike = step(0.5, state.leanHeightSpike.w);
    float heightScale = lerp(_HL_BladeHeightScale, 1.0, spike);
    float height = max(1e-4, seed.heightPhaseWidthRandom.x * state.leanHeightSpike.z * heightScale);
    float spikeWidth = 2.0 * lerp(0.065, 0.045, saturate(2.0 * state.leanHeightSpike.w - 1.0));
    float width = lerp(seed.heightPhaseWidthRandom.z, spikeWidth, spike);
    float2 lean = state.leanHeightSpike.xy;
    float yaw = seed.positionYaw.w;

    float3 scaledPosition = float3(positionOS.x * width, positionOS.y * height, positionOS.z * width);
    float3 scaledNormal = float3(normalOS.x / width, normalOS.y / height, normalOS.z / width);

    HLGrassPlacement tuft;
    tuft.positionWS = seed.positionYaw.xyz + HLTiltGrassTuft(HLYawGrassTuft(scaledPosition, yaw), lean);
    tuft.normalWS = normalize(HLTiltGrassTuft(HLYawGrassTuft(scaledNormal, yaw), lean));
    tuft.color = HLGrassColor(seed, state);
    return tuft;
}
#endif

#endif // HL_GRASS_INSTANCING_INCLUDED
