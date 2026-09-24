#ifndef HL_GRASS_INSTANCING_INCLUDED
#define HL_GRASS_INSTANCING_INCLUDED

// Grass is the tuft and socle meshes drawn indirectly: the compute writes one state per tuft and appends
// the visible ids, and this path reads each tuft's transform. Everything after it is the plant look.

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
    float4 heightWidthLean;
};

struct HLBladeState
{
    float4 leanHeightSpike;
};

StructuredBuffer<HLBladeSeed> _HL_BladeSeeds;
StructuredBuffer<HLBladeState> _HL_BladeStates;
StructuredBuffer<uint> _HL_VisibleBladeIDs;
float _HL_BladeHeightScale;
// 1 on the tuft draw, 0 on the socle draw, which lies flat on the ground
float _HL_TuftLean;

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

// positionOS and normalOS are the unit tuft or socle of GrassTuft: base on y 0, apex at y 1, base width 1.
// The tuft moves as a rigid body: scale, yaw, one tilt about its root, then the root position.
// GrassTuft.Place and PlaceNormal mirror this on the CPU.
void HLPlaceGrassBlade(float3 positionOS, float3 normalOS, uint instanceID, out float3 positionWS, out float3 normalWS)
{
    InitIndirectDrawArgs(0);
    uint bladeID = _HL_VisibleBladeIDs[GetIndirectInstanceID(instanceID)];
    HLBladeSeed seed = _HL_BladeSeeds[bladeID];
    HLBladeState state = _HL_BladeStates[bladeID];

    float spike = step(0.5, state.leanHeightSpike.w);
    float heightScale = lerp(_HL_BladeHeightScale, 1.0, spike);
    float height = max(1e-4, seed.heightWidthLean.x * state.leanHeightSpike.z * heightScale);
    float spikeWidth = 2.0 * lerp(0.065, 0.045, saturate(2.0 * state.leanHeightSpike.w - 1.0));
    float width = lerp(seed.heightWidthLean.y, spikeWidth, spike);
    float2 lean = state.leanHeightSpike.xy * _HL_TuftLean;
    float yaw = seed.positionYaw.w;

    float3 scaledPosition = float3(positionOS.x * width, positionOS.y * height, positionOS.z * width);
    float3 scaledNormal = float3(normalOS.x / width, normalOS.y / height, normalOS.z / width);
    positionWS = seed.positionYaw.xyz + HLTiltGrassTuft(HLYawGrassTuft(scaledPosition, yaw), lean);
    normalWS = normalize(HLTiltGrassTuft(HLYawGrassTuft(scaledNormal, yaw), lean));
}
#endif

#endif // HL_GRASS_INSTANCING_INCLUDED
