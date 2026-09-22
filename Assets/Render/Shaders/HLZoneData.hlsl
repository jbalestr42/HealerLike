#ifndef HL_ZONE_DATA_INCLUDED
#define HL_ZONE_DATA_INCLUDED
struct HLZone
{
    float3 position;
    float radius;
    int kind;
    float strength;
    float age;
    uint reserved;
};
// Two 16-byte lanes; no reliance on the native Metal layout of float3.
struct HLZoneStorage
{
    float4 positionRadius; // bytes 0..15
    uint4 kindStrengthAgeReserved; // bytes 16..31, bit representations
};
StructuredBuffer<HLZoneStorage> _HL_Zones;
int _HL_ZoneCount;

HLZone HLLoadZone(uint index)
{
    HLZoneStorage v = _HL_Zones[index];
    HLZone z;
    z.position = v.positionRadius.xyz;
    z.radius = v.positionRadius.w;
    z.kind = asint(v.kindStrengthAgeReserved.x);
    z.strength = asfloat(v.kindStrengthAgeReserved.y);
    z.age = asfloat(v.kindStrengthAgeReserved.z);
    z.reserved = v.kindStrengthAgeReserved.w;
    return z;
}
#endif
