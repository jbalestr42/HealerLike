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
#include "GrassTuftData.hlsl"

// A spike's half width at its base, narrowing to the second as it fully rises; GrassLayout.SpikeHalfWidth is
// the first
#define HL_SPIKE_HALF_WIDTH 0.065
#define HL_SPIKE_RISEN_HALF_WIDTH 0.045

StructuredBuffer<HLTuftSeed> _HLBladeSeeds;
StructuredBuffer<HLTuftState> _HLBladeStates;
StructuredBuffer<uint> _HLVisibleBladeIDs;
float _HLBladeHeightScale;
// 1 on the tuft draw, 0 on the socle draw, which lies flat on the ground
float _HLTuftLean;
// Per draw: ordinary meadow blades receive object shadows but only hostile spikes cast onto the carpet.
float _HLGrassSpikeShadowsOnly;

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

// The tangent turns from HL_ROOT_BEND of the lean at the root to HL_ROOT_BEND plus HL_TIP_BEND at the tip, so the
// chord from root to tip tilts by the lean itself; GrassTuft.RootBend and TipBend
#define HL_ROOT_BEND 0.4
#define HL_TIP_BEND 1.2

float HLGrassSinc(float x)
{
    return abs(x) < 1e-4 ? 1.0 - x * x / 6.0 : sin(x) / x;
}

// The point a height share t along the bent axis of a tuft this tall, an arc of the tuft's own length
float3 HLGrassSpine(float2 lean, float height, float t)
{
    float angle = length(lean);
    float2 heading = angle > 1e-5 ? lean / angle : float2(0.0, 0.0);
    float turn = angle * HL_TIP_BEND * t;
    float mean = angle * (HL_ROOT_BEND + 0.5 * HL_TIP_BEND * t);
    float chord = height * t * HLGrassSinc(0.5 * turn);
    float across = chord * sin(mean);
    return float3(heading.x * across, chord * cos(mean), heading.y * across);
}

// positionOS and normalOS are the unit tuft or socle of GrassTuft: base on y 0, apex at y 1, base width 1.
// Scale and yaw, then the tuft bends: the section at each height rides the spine and turns with its tangent.
// GrassTuft.Place and PlaceNormal mirror this on the CPU.
void HLPlaceGrassTuft(float3 positionOS, float3 normalOS, uint instanceID, out float3 positionWS,
                      out float3 normalWS, out float2 appearance)
{
    InitIndirectDrawArgs(0);
    uint tuftID = _HLVisibleBladeIDs[GetIndirectInstanceID(instanceID)];
    HLTuftSeed seed = _HLBladeSeeds[tuftID];
    HLTuftState state = _HLBladeStates[tuftID];

    float spike = step(0.5, state.leanHeightSpike.w);
    appearance = float2(saturate(positionOS.y), 1.0 - spike);
    float heightScale = lerp(_HLBladeHeightScale, 1.0, spike);
    float height = max(1e-4, seed.heightWidthLean.x * state.leanHeightSpike.z * heightScale);
    float risen = saturate(2.0 * state.leanHeightSpike.w - 1.0);
    float spikeWidth = 2.0 * lerp(HL_SPIKE_HALF_WIDTH, HL_SPIKE_RISEN_HALF_WIDTH, risen);
    float width = lerp(seed.heightWidthLean.y, spikeWidth, spike);
    float2 lean = state.leanHeightSpike.xy * _HLTuftLean;
    float yaw = seed.positionYaw.w;

    float t = positionOS.y;
    float2 tangentLean = lean * (HL_ROOT_BEND + HL_TIP_BEND * t);
    float3 section = HLYawGrassTuft(float3(positionOS.x * width, 0.0, positionOS.z * width), yaw);
    float3 scaledNormal = float3(normalOS.x / width, normalOS.y / height, normalOS.z / width);
    positionWS = seed.positionYaw.xyz + HLGrassSpine(lean, height, t) + HLTiltGrassTuft(section, tangentLean);
    normalWS = normalize(HLTiltGrassTuft(HLYawGrassTuft(scaledNormal, yaw), tangentLean));
}
#endif

#endif // HL_GRASS_INSTANCING_INCLUDED
