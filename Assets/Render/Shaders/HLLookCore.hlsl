#ifndef HL_LOOK_CORE_INCLUDED
#define HL_LOOK_CORE_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Wave-0 scaffold: the ABI below (globals, HL_G, every prototype) is frozen.
// The bodies are placeholders so the header compiles standalone; track T1 fills them
// in per look.md's five normative rules. The core must not depend on Light, textures,
// instance-buffer layout, mesh deformation or zone data; the adapter supplies illum.

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

// TODO(T1): hash / dash noise / line coverage per look.md rules 2 and 3.
float HLHash11(float p)
{
    return 0.0;
}

float HLDashNoise(float x)
{
    return 0.0;
}

float HLLine(float coord, float warp, float spacing,
             float inkWarp, float inkWarpFreq, float inkWidth)
{
    return 0.0;
}

// TODO(T1): shadowMask = 1 - step(HL_G(_HLToonThreshold, default), saturate(illum)); equality is lit.
float HLShadowMask(float illum)
{
    return 0.0;
}

// TODO(T1): banded fog per look.md rule 4.
float HLFogFactor(float3 positionWS)
{
    return 0.0;
}

float3 HLApplyBandedFog(float3 positionWS, float3 color)
{
    return color;
}

// TODO(T1): binary toon fill plus hatch, gated by the same shadow mask.
float3 HLShadeSurface(float3 positionWS, float illum, float3 baseColor)
{
    return baseColor;
}

// Final form is HLApplyBandedFog(positionWS, HLShadeSurface(positionWS, illum, baseColor)).
float3 HLEvaluateSurface(float3 positionWS, float illum, float3 baseColor)
{
    return baseColor;
}

// TODO(T1): pixel-width world expansion per look.md rule 5. Width 0 must return the input.
float3 HLOutlineExtrude(float3 positionWS, float3 outlineNormalWS)
{
    return positionWS;
}

// TODO(T1): fog the resolved outline colour using the original surface world position.
float3 HLEvaluateOutline(float3 unextrudedPositionWS)
{
    return HL_G(_HLOutlineColor, float4(0.0, 0.0, 0.0, 1.0)).rgb;
}
#endif // HL_LOOK_CORE_INCLUDED
