#ifndef HL_ZONE_DATA_INCLUDED
#define HL_ZONE_DATA_INCLUDED
// The zone buffer's capacity, ZonePacker.MaxZones on the C# side
#define HL_MAX_ZONES 64
// One per ZoneKind member, with its value
#define HL_ZONE_NONE 0
#define HL_ZONE_HEAL 1
#define HL_ZONE_HOSTILE 2
#define HL_ZONE_RANGE 3
#define HL_ZONE_BRUISE 4
#define HL_ZONE_LAUNCH 5
#define HL_ZONE_TRAMPLE 6
#define HL_ZONE_SHOCK 7

struct HLZone
{
    float3 position;
    float radius;
    int kind;
    float strength;
    float age;
    uint reserved;
    float2 direction; // Launch heading, decoded from reserved: a full turn from +X toward +Z
};
// Two 16-byte lanes; no reliance on the native Metal layout of float3.
struct HLZoneStorage
{
    float4 positionRadius; // bytes 0..15
    uint4 kindStrengthAgeReserved; // bytes 16..31, bit representations
};
StructuredBuffer<HLZoneStorage> _HLZones;
int _HLZoneCount;

HLZone HLLoadZone(uint index)
{
    HLZoneStorage v = _HLZones[index];
    HLZone z;
    z.position = v.positionRadius.xyz;
    z.radius = v.positionRadius.w;
    z.kind = asint(v.kindStrengthAgeReserved.x);
    z.strength = asfloat(v.kindStrengthAgeReserved.y);
    z.age = asfloat(v.kindStrengthAgeReserved.z);
    z.reserved = v.kindStrengthAgeReserved.w;
    float angle = (float)z.reserved * (6.28318530718 / 4294967296.0);
    z.direction = float2(cos(angle), sin(angle));
    // Bloom belongs to the shared loader, so the grass footprint and ring stay identical.
    if (z.kind == HL_ZONE_HEAL)
    {
        z.radius *= saturate(z.age / 0.3);
    }

    return z;
}
#endif
