#ifndef HL_GROUND_COMMON_INCLUDED
#define HL_GROUND_COMMON_INCLUDED
// The ground's shared maths: stamps, wind and the texel spring. GroundStamp, GroundWind and GroundSpring mirror
// each function on the C# side.

// The rim wobble's spatial frequency, GroundStamp.RimFrequency
#define HL_GROUND_RIM_FREQUENCY float2(9.7, 6.3)

// The kinds of GroundStampKind
#define HL_GROUND_DISC 0
#define HL_GROUND_FRONT 1
#define HL_GROUND_BODY 2

// Disc and front: centreRadius xy centre in world XZ, z radius, w front half width. Disc push: x turn from outward,
// w push. Front push: xy heading, z push along it (zero blows all round), w push outward. Body: centreRadius the
// first end (x, z, height, radius), push the second end (x, z, height, margin). shape: x flatness, y disc edge
// share, z rim wobble share, w kind. response: x grass height and y lean for a body, z held share, w kick.
struct HLGroundStamp
{
    float4 centreRadius;
    float4 push;
    float4 shape;
    float4 response;
};

float2 HLGroundTurn(float2 v, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// The square a stamp can write into: its centre in world XZ and half its side
void HLGroundStampBounds(HLGroundStamp stamp, out float2 centre, out float reach)
{
    if (round(stamp.shape.w) == HL_GROUND_BODY)
    {
        centre = 0.5 * (stamp.centreRadius.xy + stamp.push.xy);
        reach = 0.5 * distance(stamp.centreRadius.xy, stamp.push.xy) + stamp.centreRadius.w + stamp.push.w;
        return;
    }

    centre = stamp.centreRadius.xy;
    reach = stamp.centreRadius.z + stamp.centreRadius.w;
}

// A capsule's pressure on the grass: flat where the body sits low over it, leaning away beside it
float3 HLGroundBodyValue(HLGroundStamp stamp, float2 p)
{
    float2 start = stamp.centreRadius.xy;
    float2 axis = stamp.push.xy - start;
    float t = saturate(dot(p - start, axis) / max(dot(axis, axis), 1e-6));
    float2 nearest = start + axis * t;
    float height = lerp(stamp.centreRadius.z, stamp.push.z, t);
    float radius = stamp.centreRadius.w;
    float2 delta = p - nearest;
    float distanceToAxis = length(delta);
    float2 outward = distanceToAxis > 1e-5 ? delta / distanceToAxis : float2(0.0, 0.0);
    float underside = height - sqrt(max(radius * radius - distanceToAxis * distanceToAxis, 0.0));
    float contact = saturate((stamp.response.x - underside) / stamp.response.x);
    float near = 1.0 - smoothstep(radius, radius + stamp.push.w, distanceToAxis);
    float covered = 1.0 - smoothstep(0.6 * radius, radius, distanceToAxis);
    return float3(outward * (stamp.response.y * contact * near), stamp.shape.x * contact * covered);
}

// xy lean in radians and z flatness the stamp adds at a world XZ point
float3 HLGroundStampValue(HLGroundStamp stamp, float2 p)
{
    float kind = round(stamp.shape.w);
    if (kind == HL_GROUND_BODY)
    {
        return HLGroundBodyValue(stamp, p);
    }

    float2 delta = p - stamp.centreRadius.xy;
    float distanceToCentre = length(delta);
    float2 outward = distanceToCentre > 1e-5 ? delta / distanceToCentre : float2(0.0, 0.0);
    float radius = stamp.centreRadius.z;
    float band = stamp.centreRadius.w;
    if (kind == HL_GROUND_FRONT)
    {
        float front = 1.0 - smoothstep(0.0, band, abs(distanceToCentre - radius));
        // A heading ahead of the centre only; a ring with no push along it blows all round
        if (stamp.push.z != 0.0)
        {
            front *= saturate(dot(outward, stamp.push.xy));
        }

        return float3((stamp.push.xy * stamp.push.z + outward * stamp.push.w) * front, stamp.shape.x * front);
    }

    float rim = radius * (1.0 - stamp.shape.z * (0.5 + 0.5 * sin(dot(p, HL_GROUND_RIM_FREQUENCY))));
    float weight = 1.0 - smoothstep(rim * (1.0 - stamp.shape.y), rim, distanceToCentre);
    return float3(HLGroundTurn(outward, stamp.push.x) * (stamp.push.w * weight), stamp.shape.x * weight);
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

// spring: x stiffness, y damping, z neighbour pull, w max lean. state: xy lean, zw its velocity. force is the
// kicked acceleration.
float4 HLGroundSpringStep(float4 state, float2 target, float2 neighbourMean, float2 force, float step,
                          float4 spring)
{
    float2 lean = state.xy;
    float2 velocity = state.zw;
    float2 acceleration = spring.x * (target - lean) - spring.y * velocity + spring.z * (neighbourMean - lean)
        + force;
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
