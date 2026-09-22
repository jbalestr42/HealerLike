Shader "HL/Grass/BladeAndCone"
{
    Properties
    {
        _BaseColor("Instance Tint", Color) = (1,1,1,1)
        [HideInInspector] _HL_Cull("Cull", Float) = 0
        [HideInInspector] _HL_Cone("Cone", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_HL_Cull] ZWrite On
        HLSLINCLUDE
        #pragma target 4.5
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:HLGrassSetup
        #include "HLGrassData.hlsl"
        ENDHLSL
        Pass
        {
            Name "HLGrassForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex HLGrassVertex
            #pragma fragment HLGrassFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "HLGrassDepth"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex HLGrassVertex
            #pragma fragment HLGrassDepth
            ENDHLSL
        }
        Pass
        {
            Name "HLGrassNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex HLGrassVertex
            #pragma fragment HLGrassNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
