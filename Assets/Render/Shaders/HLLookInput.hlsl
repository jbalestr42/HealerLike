#ifndef HL_LOOK_INPUT_INCLUDED
#define HL_LOOK_INPUT_INCLUDED
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float _HLNormalEdges;
CBUFFER_END

#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(HLPerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _HLNormalEdges)
UNITY_INSTANCING_BUFFER_END(HLPerInstance)
#endif

float4 HLGetBaseColor()
{
#if defined(UNITY_INSTANCING_ENABLED)
    return UNITY_ACCESS_INSTANCED_PROP(HLPerInstance, _BaseColor);
#else
    return _BaseColor;
#endif
}
float HLGetNormalEdges()
{
#if defined(UNITY_INSTANCING_ENABLED)
    return saturate(UNITY_ACCESS_INSTANCED_PROP(HLPerInstance, _HLNormalEdges));
#else
    return saturate(_HLNormalEdges);
#endif
}
#endif // HL_LOOK_INPUT_INCLUDED
