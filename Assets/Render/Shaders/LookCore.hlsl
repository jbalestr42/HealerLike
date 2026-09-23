#ifndef HL_LOOK_CORE_INCLUDED
#define HL_LOOK_CORE_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Shared HL look for primitive, grass and stone adapters.
// Include URP Core.hlsl before this file. Fragment shading uses derivatives.
// HLLookController publishes global tuning; material colour stays in HLLookInput.hlsl.
// Keep all look globals outside UnityPerMaterial and shader Properties.

// Full global ABI: outside UnityPerMaterial; absent from Properties.
float4 _HLShadowTint;
float  _HLShadowStrength;
float  _HLToonThreshold;
float4 _HLOutlineColor;
float  _HLOutlineWidthPixels;
float4 _HLFogColor;
float  _HLFogStart;
float  _HLFogEnd;
float  _HLFogBands;
float  _HLInkStrength;
float  _HLInkScale;
float  _HLInkWidth;
float  _HLInkStart;
float  _HLInkRange;
float  _HLDensityMul;
float  _HLInkWarp;
float  _HLInkWarpFreq;
float  _HLDashAmount;
float  _HLDashScale;
float  _HLInkDistStart;
float  _HLInkFarSpacing;
float  _HLLookApplied;
// Additive beauty controls. Stage owns grid bounds and tip strength; zero disables both.
float4 _HLGridOrigin; // xyz: world-space minimum board corner
float _HLGridCell; // square cell size in world units
float4 _HLGridExtent; // x/z: board width/depth in world units
float _HLGridStrength; // 0..1, recommended .12
float _HLTipLight; // 0..1, recommended .12; consumed by grass adapter
float _HLInkSpacingPixels;
float4 _HLKeyLightDir; // World-space direction toward main light, w=0; zero when absent.

#define HL_G(uniformName, fallbackValue) \
    ((_HLLookApplied > 0.5) ? (uniformName) : (fallbackValue))

// Returns srgb unchanged under UNITY_COLORSPACE_GAMMA, otherwise the linear working value.
float3 HLWorkingColor(float3 srgb)
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return srgb;
#else
    return SRGBToLinear(srgb);
#endif
}

// HLLookController uploads every global before setting _HLLookApplied to 1.
// When the flag is 0, use the complete baked HL defaults.
// Keep HLLookSettings defaults and shader fallback values identical.
#define HL_DEF_SHADOWTINT float4(HLWorkingColor(float3(43,75,143)/255.0),1)
#define HL_DEF_OUTLINECOLOR float4(HLWorkingColor(float3(24,38,63)/255.0),1)
#define HL_DEF_FOGCOLOR float4(HLWorkingColor(float3(191,210,224)/255.0),1)
#define HL_DEF_SHADOWSTRENGTH 0.65
#define HL_DEF_TOONTHRESHOLD 0.5
#define HL_DEF_OUTLINEWIDTHPIXELS 1.0
// Provisional world-unit fog distances; calibrate against the gameplay camera.
#define HL_DEF_FOGSTART 20.0
#define HL_DEF_FOGEND 60.0
#define HL_DEF_FOGBANDS 6.0
#define HL_DEF_INKSTRENGTH 1.0
#define HL_DEF_INKSPACINGPIXELS 3.5
#define HL_DEF_INKSCALE 0.05
#define HL_DEF_INKWIDTH 0.001
#define HL_DEF_INKSTART 0.0
#define HL_DEF_INKRANGE 1.0
#define HL_DEF_DENSITYMUL 0.55
#define HL_DEF_INKWARP 0.006
#define HL_DEF_INKWARPFREQ 2.44
#define HL_DEF_DASHAMOUNT 0.1
#define HL_DEF_DASHSCALE 0.01
#define HL_DEF_INKDISTSTART 10.0
#define HL_DEF_INKFARSPACING 0.06

float HLHash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float HLDashNoise(float x)
{
    float i = floor(x), f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(HLHash11(i), HLHash11(i + 1.0), f);
}

float HLLine(float coord, float warp, float spacing,
             float inkWarp, float inkWarpFreq, float inkWidth)
{
    spacing = max(spacing, 1e-4);
    coord += (sin(warp * inkWarpFreq) + 0.5 * sin(warp * inkWarpFreq * 2.7)) * inkWarp;
    float u = coord / spacing;
    float d = min(frac(u), 1.0 - frac(u));
    float footprint = max(fwidth(u), 1e-6);
    float aa = min(footprint, 0.25);
    float hw = clamp(inkWidth, 0.0, 0.24 * spacing) / spacing;
    // Fade unresolved strokes above the two-pixel frequency limit.
    float resolved = 1.0 - smoothstep(0.25, 0.5, footprint);
    return (1.0 - smoothstep(hw, hw + aa, d)) * resolved;
}

float HLShadowMask(float illum)
{
    return 1.0 - step(HL_G(_HLToonThreshold, HL_DEF_TOONTHRESHOLD), saturate(illum));
}

float HLFogFactor(float3 positionWS)
{
    float bands = max(1.0, floor(HL_G(_HLFogBands, HL_DEF_FOGBANDS) + 0.5));
    float start = HL_G(_HLFogStart, HL_DEF_FOGSTART);
    float end = HL_G(_HLFogEnd, HL_DEF_FOGEND);
    float f = saturate((distance(positionWS, GetCameraPositionWS()) - start) / max(0.001, end - start));
    // Static world-space threshold noise: stable while plants move, no invented clock.
    // Envelope preserves exactly clear near and fully fogged far endpoints.
    float noise = (HLDashNoise(dot(positionWS.xz, float2(.37, .21))) * .65 +
                   HLDashNoise(dot(positionWS.xz, float2(-.19, .43)) + 17.3) * .35) * 2.0 - 1.0;
    float warped = saturate(f + noise * .32 / bands * saturate(f * bands) * saturate((1.0 - f) * bands));
    return floor(warped * bands) / bands;
}

float3 HLApplyBandedFog(float3 positionWS, float3 color)
{
    return lerp(color, HL_G(_HLFogColor, HL_DEF_FOGCOLOR).rgb, HLFogFactor(positionWS));
}

// Shared HL surface shading; positionWS and baseColor are adapter inputs.
// Illumination is remapped main-light facing multiplied by shadow attenuation.
// Hatch is restricted to the same binary shadow mask as the toon fill.
float3 HLShadeSurface(float3 positionWS, float illum, float3 baseColor)
{
    illum = saturate(illum);
    float shadowMask = HLShadowMask(illum);
    // Deep cast shadows converge to the authored ultramarine, instead of retaining
    // enough green base colour to read as grey/teal. Strength controls the toon boundary.
    float tintStrength = lerp(HL_G(_HLShadowStrength, HL_DEF_SHADOWSTRENGTH), 1.0,
        saturate(1.0 - illum / max(.001, HL_G(_HLToonThreshold, HL_DEF_TOONTHRESHOLD))));
    float3 shadowColor = lerp(baseColor, HL_G(_HLShadowTint, HL_DEF_SHADOWTINT).rgb, tintStrength);
    float3 color = lerp(baseColor, shadowColor, shadowMask);
    float tone = saturate(((1.0 - illum) - HL_G(_HLInkStart, HL_DEF_INKSTART)) /
                          max(0.001, HL_G(_HLInkRange, HL_DEF_INKRANGE)));
    float hcoord = dot(positionWS, normalize(float3(1.0, 0.35, 0.6)));
    float hwarp = dot(positionWS, normalize(float3(-0.6, 0.0, 1.0)));
    float spacing = HL_G(_HLInkScale, HL_DEF_INKSCALE) * lerp(1.0, HL_G(_HLDensityMul, HL_DEF_DENSITYMUL), tone);
    float dist = distance(positionWS, GetCameraPositionWS());
    spacing *= 1.0 + HL_G(_HLInkFarSpacing, HL_DEF_INKFARSPACING) *
        max(0.0, dist / max(0.001, HL_G(_HLInkDistStart, HL_DEF_INKDISTSTART)) - 1.0);
    // Projected hatch-coordinate footprint follows camera distance, FOV and render scale.
    // Pixel mode bypasses tone compression so dense shadows remain readable.
    float pixelSpacing = HL_G(_HLInkSpacingPixels, HL_DEF_INKSPACINGPIXELS);
    float footprintWS = length(float2(ddx(hcoord), ddy(hcoord)));
    if (pixelSpacing > 0.0) spacing = footprintWS * pixelSpacing;
    spacing = max(spacing, 1e-4);
    // Derivatives must run for every lane, including lit fragments.
    float ink = HLLine(hcoord, hwarp, spacing, HL_G(_HLInkWarp, HL_DEF_INKWARP),
                       HL_G(_HLInkWarpFreq, HL_DEF_INKWARPFREQ), HL_G(_HLInkWidth, HL_DEF_INKWIDTH));
    float lineId = floor(hcoord / spacing + 0.5);
    float dn = HLDashNoise(hwarp / max(0.001, HL_G(_HLDashScale, HL_DEF_DASHSCALE)) + lineId * 7.31);
    float dashAmount = HL_G(_HLDashAmount, HL_DEF_DASHAMOUNT);
    ink *= smoothstep(dashAmount, dashAmount + 0.08, dn);
    ink = saturate(ink * shadowMask * step(0.004, tone) * HL_G(_HLInkStrength, HL_DEF_INKSTRENGTH));
    return lerp(color, HL_G(_HLOutlineColor, HL_DEF_OUTLINECOLOR).rgb, ink * 0.65);
}

float3 HLEvaluateSurface(float3 positionWS, float illum, float3 baseColor)
{
    return HLApplyBandedFog(positionWS, HLShadeSurface(positionWS, illum, baseColor));
}

// Direct clip-space adapter avoids a lossy world-space round trip at distant silhouettes.
float4 HLOutlineClip(float3 positionWS, float3 outlineNormalWS, float widthMultiplier)
{
    float4 pCS = TransformWorldToHClip(positionWS);
    float lengthSquared = dot(outlineNormalWS, outlineNormalWS);
    if (lengthSquared < 1e-12 || pCS.w <= 1e-5) return pCS;
    float4 nCS = mul(UNITY_MATRIX_VP, float4(outlineNormalWS * rsqrt(lengthSquared), 0));
    // Quotient derivative includes perspective and off-axis projection. Normalize in
    // pixel space, then offset clip XY without changing depth (orthographic works too).
    float2 size = max(_ScaledScreenParams.xy, float2(1, 1));
    float2 directionPixels = (nCS.xy * pCS.w - pCS.xy * nCS.w) * size;
    float directionLength = length(directionPixels);
    if (directionLength < 1e-6) return pCS;
    pCS.xy += directionPixels / directionLength *
        HL_G(_HLOutlineWidthPixels, HL_DEF_OUTLINEWIDTHPIXELS) * max(0.0, widthMultiplier) * 2.0 / size * pCS.w;
    return pCS;
}

// Frozen world-space API for existing adapters.
float3 HLOutlineExtrude(float3 positionWS, float3 outlineNormalWS)
{
    float4 world = mul(UNITY_MATRIX_I_VP, HLOutlineClip(positionWS, outlineNormalWS, 1.0));
    return world.xyz / world.w;
}

// Ground adapter only: never call from shared surface shading or plants/rocks.
float3 HLApplyBattlefieldGrid(float3 positionWS, float3 color)
{
    float2 local = positionWS.xz - _HLGridOrigin.xz;
    float2 extent = _HLGridExtent.xz;
    float cell = max(_HLGridCell, .001);
    float2 coord = local / cell;
    float2 footprint = max(fwidth(coord), float2(1e-5, 1e-5));
    float2 lineDistance = abs(frac(coord + .5) - .5) / footprint;
    float gridLine = 1.0 - smoothstep(.25, 1.0, min(lineDistance.x, lineDistance.y));
    float inside = step(0.0, local.x) * step(0.0, local.y) *
                   step(local.x, extent.x) * step(local.y, extent.y);
    float enabled = step(.001, _HLGridCell) * step(.001, min(extent.x, extent.y));
    float farFade = 1.0 - smoothstep(0.0, 1.0, saturate(
        (distance(positionWS, GetCameraPositionWS()) - HL_G(_HLFogStart, HL_DEF_FOGSTART)) /
        max(.001, HL_G(_HLFogEnd, HL_DEF_FOGEND) - HL_G(_HLFogStart, HL_DEF_FOGSTART))));
    float resolved = 1.0 - smoothstep(.25, .5, max(footprint.x, footprint.y));
    return lerp(color, HLWorkingColor(float3(.82, .89, .83)),
        gridLine * inside * enabled * resolved * farFade * saturate(_HLGridStrength));
}

// Grass adapter calls before fog; bladeHeight01 is root=0, tip=1.
float3 HLApplyTipLight(float3 color, float bladeHeight01, float illum)
{
    float amount = smoothstep(.65, 1.0, saturate(bladeHeight01)) *
                   (1.0 - HLShadowMask(illum)) * saturate(_HLTipLight);
    return lerp(color, HLWorkingColor(float3(.88, .96, .65)), amount);
}

float3 HLEvaluateOutline(float3 unextrudedPositionWS)
{
    return HLApplyBandedFog(unextrudedPositionWS, HL_G(_HLOutlineColor, HL_DEF_OUTLINECOLOR).rgb);
}
#endif // HL_LOOK_CORE_INCLUDED
