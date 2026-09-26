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
#define HL_GROUND_AURA 3
#define HL_GROUND_STREAK 4

// Disc and front: centreRadius xy centre in world XZ, z radius, w front half width. Disc push: x turn from outward,
// w push. Front push: w push outward, the front a ring travelling out. Body: centreRadius the
// first end (x, z, height, radius), push the second end (x, z, height, margin). shape: x flatness, y disc edge
// share, z rim wobble share, w kind. response: x grass height and y lean for a body, z held share, w kick.
// Aura: centreRadius like a disc, push x ash, y vitality, z light (frost below zero, glow above), w blight.
// Streak: push like an aura; centreRadius xy start, z half width, w zigzag swing; response xy end; shape z turns
// per world unit.
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
    if (round(stamp.shape.w) == HL_GROUND_STREAK)
    {
        centre = 0.5 * (stamp.centreRadius.xy + stamp.response.xy);
        reach = 0.5 * distance(stamp.centreRadius.xy, stamp.response.xy) + stamp.centreRadius.z
            + stamp.centreRadius.w;
        return;
    }

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
    // A trail has no radius; the guard keeps smoothstep off equal edges, which is 0/0 on some GPUs
    float covered = 1.0 - smoothstep(0.6 * radius, max(radius, 1e-4), distanceToAxis);
    return float3(outward * (stamp.response.y * contact * near), stamp.shape.x * contact * covered);
}

// A disc's cover at a point, its rim wobbling inward by the wobble share
float HLGroundDiscWeight(HLGroundStamp stamp, float2 p)
{
    float radius = stamp.centreRadius.z;
    float rim = radius * (1.0 - stamp.shape.z * (0.5 + 0.5 * sin(dot(p, HL_GROUND_RIM_FREQUENCY))));
    return 1.0 - smoothstep(rim * (1.0 - stamp.shape.y), rim, length(p - stamp.centreRadius.xy));
}

// A streak's cover at a point: the distance to its zigzag path, thinning toward its end
float HLGroundStreakWeight(HLGroundStamp stamp, float2 p)
{
    float2 start = stamp.centreRadius.xy;
    float2 axis = stamp.response.xy - start;
    float length = max(sqrt(dot(axis, axis)), 1e-5);
    float2 along = axis / length;
    float2 delta = p - start;
    float t = saturate(dot(delta, along) / length);
    float side = along.x * delta.y - along.y * delta.x;
    // Crosses the axis at the start, so the bolt leaves from where it struck; measured across its slanted legs
    float turn = t * length * stamp.shape.z;
    float zigzag = 1.0 - 4.0 * abs(frac(turn + 0.25) - 0.5);
    float slope = 4.0 * stamp.centreRadius.w * stamp.shape.z;
    float offset = abs(side - stamp.centreRadius.w * zigzag) / sqrt(1.0 + slope * slope);
    float beyond = abs(dot(delta, along) - t * length);
    float distanceToPath = sqrt(offset * offset + beyond * beyond);
    float width = stamp.centreRadius.z * (1.0 - 0.5 * t);
    return 1.0 - smoothstep(width * (1.0 - stamp.shape.y), width, distanceToPath);
}

// What an aura or a streak asks of the ground state, scaled by its cover: x ash, y vitality, z light, w blight
float4 HLGroundStampState(HLGroundStamp stamp, float2 p)
{
    float kind = round(stamp.shape.w);
    if (kind == HL_GROUND_STREAK)
    {
        return stamp.push * HLGroundStreakWeight(stamp, p);
    }

    if (kind != HL_GROUND_AURA)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    return stamp.push * HLGroundDiscWeight(stamp, p);
}

// xy lean in radians and z flatness the stamp adds at a world XZ point
float3 HLGroundStampValue(HLGroundStamp stamp, float2 p)
{
    float kind = round(stamp.shape.w);
    if (kind == HL_GROUND_BODY)
    {
        return HLGroundBodyValue(stamp, p);
    }

    if (kind == HL_GROUND_AURA || kind == HL_GROUND_STREAK)
    {
        return float3(0.0, 0.0, 0.0);
    }

    float2 delta = p - stamp.centreRadius.xy;
    float distanceToCentre = length(delta);
    float2 outward = distanceToCentre > 1e-5 ? delta / distanceToCentre : float2(0.0, 0.0);
    float radius = stamp.centreRadius.z;
    float band = stamp.centreRadius.w;
    if (kind == HL_GROUND_FRONT)
    {
        float front = 1.0 - smoothstep(0.0, band, abs(distanceToCentre - radius));
        return float3(outward * (stamp.push.w * front), stamp.shape.x * front);
    }

    float weight = HLGroundDiscWeight(stamp, p);
    return float3(HLGroundTurn(outward, stamp.push.x) * (stamp.push.w * weight), stamp.shape.x * weight);
}

// wind: xy prevailing direction, z strength in radians, w time in seconds
float2 HLGroundWindLean(float2 p, float4 wind)
{
    float2 direction = wind.xy;
    float2 across = float2(-direction.y, direction.x);
    float time = wind.w;
    float along = dot(p, direction);
    float side = dot(p, across);
    float front = sin(along * 0.9 - time * 1.7 + 1.3 * sin(side * 0.6 + time * 0.4));
    float flutter = sin(dot(p, float2(0.67, 0.43)) * 1.3 + time * 1.1);
    return (direction * (0.55 + 0.45 * front) + across * (0.3 * flutter)) * wind.z;
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
    // At the cap the grass stops leaning further: the outward part of its velocity goes, so it swings back
    float length = sqrt(dot(lean, lean));
    if (length > spring.w)
    {
        float2 outward = lean / length;
        velocity -= max(dot(velocity, outward), 0.0) * outward;
        lean = outward * spring.w;
    }

    return float4(lean, velocity);
}

// rates: x toward a higher flatness, y toward a lower one, per second
float HLGroundCrushStep(float crush, float target, float step, float2 rates)
{
    float rate = target > crush ? rates.x : rates.y;
    return crush + (target - crush) * (1.0 - exp(-rate * step));
}

// rates: x toward more ash, y back from ash, z toward the asked vitality, w back to neutral vitality; lightRates:
// x toward more light or frost, y back to none, z toward more blight, w back from it, all per second. aura is
// the summed HLGroundStampState. state: x ash, y vitality, z light, w blight.
float4 HLGroundStateStep(float4 state, float4 aura, float step, float4 rates, float4 lightRates)
{
    float4 asked = float4(saturate(aura.x), clamp(aura.yz, -1.0, 1.0), saturate(aura.w));
    float ashRate = asked.x > state.x ? rates.x : rates.y;
    float vitalityRate = abs(asked.y) > abs(state.y) ? rates.z : rates.w;
    float lightRate = abs(asked.z) > abs(state.z) ? lightRates.x : lightRates.y;
    float blightRate = asked.w > state.w ? lightRates.z : lightRates.w;
    float4 rate = float4(ashRate, vitalityRate, lightRate, blightRate);
    float4 next = state + (asked - state) * (1.0 - exp(-rate * step));
    return float4(saturate(next.x), clamp(next.yz, -1.0, 1.0), saturate(next.w));
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
