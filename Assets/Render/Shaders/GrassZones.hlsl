#ifndef HL_GRASS_ZONES_INCLUDED
#define HL_GRASS_ZONES_INCLUDED
#include "ZoneData.hlsl"

float HLGrassZoneOnset(HLZone z)
{
    return saturate(z.strength) * smoothstep(0.0, 0.12, z.age);
}

float HLGrassZoneWeight(HLZone z, float2 p)
{
    float2 delta = p - z.position.xz;
    if (z.radius <= 0.0 || any(abs(delta) > z.radius))
    {
        return 0.0;
    }

    float edge = min(0.15, 0.25 * z.radius);
    return HLGrassZoneOnset(z) * (1.0 - smoothstep(z.radius - edge, z.radius, length(delta)));
}
#endif
