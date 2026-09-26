#ifndef HL_GROUND_COMMON_INCLUDED
#define HL_GROUND_COMMON_INCLUDED
// The ground's shared maths: stamps, wind and the texel spring. GroundStamp, GroundWind and GroundSpring mirror
// each function on the C# side.

// The rim wobble's spatial frequency, GroundStamp.RimFrequency
#define HL_GROUND_RIM_FREQUENCY float2(9.7, 6.3)

// centreRadius: xy centre in world XZ, z radius, w half width of a ring front, zero for a disc
// push: xy heading, z lean along it, w lean away from the centre, in radians
// shape: x flatness, y the share of the radius a disc edge fades over, z rim wobble share, w ahead-only
struct HLGroundStamp
{
    float4 centreRadius;
    float4 push;
    float4 shape;
};

// xy lean in radians and z flatness the stamp adds at a world XZ point
float3 HLGroundStampValue(HLGroundStamp stamp, float2 p)
{
    float2 delta = p - stamp.centreRadius.xy;
    float distance = length(delta);
    float2 outward = distance > 1e-5 ? delta / distance : float2(0.0, 0.0);
    float radius = stamp.centreRadius.z;
    float band = stamp.centreRadius.w;
    float weight;
    if (band > 0.0)
    {
        weight = 1.0 - smoothstep(0.0, band, abs(distance - radius));
    }
    else
    {
        float rim = radius * (1.0 - stamp.shape.z * (0.5 + 0.5 * sin(dot(p, HL_GROUND_RIM_FREQUENCY))));
        weight = 1.0 - smoothstep(rim * (1.0 - stamp.shape.y), rim, distance);
    }

    if (stamp.shape.w > 0.0)
    {
        weight *= saturate(dot(outward, stamp.push.xy));
    }

    float2 lean = (stamp.push.xy * stamp.push.z + outward * stamp.push.w) * weight;
    return float3(lean, stamp.shape.x * weight);
}

// wind: xy prevailing direction, z strength in radians, w time in seconds. gust is the pulses' lean.
float2 HLGroundWindLean(float2 p, float4 wind, float2 gust)
{
    float2 direction = wind.xy;
    float2 across = float2(-direction.y, direction.x);
    float time = wind.w;
    float along = dot(p, direction);
    float side = dot(p, across);
    float front = sin(along * 0.9 - time * 1.7 + 1.3 * sin(side * 0.6 + time * 0.4));
    float flutter = sin(dot(p, float2(0.67, 0.43)) * 1.3 + time * 1.1);
    return (direction * (0.55 + 0.45 * front) + across * (0.3 * flutter)) * wind.z + gust;
}

float2 HLGroundCapLean(float2 lean, float limit)
{
    return lean * min(1.0, limit / max(length(lean), 1e-5));
}

// spring: x stiffness, y damping, z coupling stiffness, w max lean. state: xy lean, zw its velocity.
float4 HLGroundSpringStep(float4 state, float2 target, float2 neighbourMean, float step, float4 spring)
{
    float2 lean = state.xy;
    float2 velocity = state.zw;
    float2 acceleration = spring.x * (target - lean) - spring.y * velocity + spring.z * (neighbourMean - lean);
    velocity += acceleration * step;
    lean += velocity * step;
    return float4(HLGroundCapLean(lean, spring.w), velocity);
}

// rates: x toward a higher flatness, y toward a lower one, per second
float HLGroundCrushStep(float crush, float target, float step, float2 rates)
{
    float rate = target > crush ? rates.x : rates.y;
    return crush + (target - crush) * (1.0 - exp(-rate * step));
}

// uv on the ground textures of a world XZ point; rect is GroundVolume.ShaderRect
float2 HLGroundUV(float2 p, float4 rect)
{
    return (p - rect.xy) * rect.zw;
}

// One inside the ground, easing to zero over fade world units toward its edge and beyond it
float HLGroundCoverage(float2 uv, float4 rect, float fade)
{
    float2 inside = min(uv, 1.0 - uv) / rect.zw;
    return saturate(min(inside.x, inside.y) / fade);
}
#endif
