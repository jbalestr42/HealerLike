#ifndef HL_GRASS_TUFT_DATA_INCLUDED
#define HL_GRASS_TUFT_DATA_INCLUDED
// One tuft's seed, written once by GrassField, and the state the compute writes every frame; the C# side is
// TuftSeed and TuftState
struct HLTuftSeed
{
    float4 positionYaw;
    float4 heightWidthLean;
};

struct HLTuftState
{
    float4 leanHeightSpike;
};
#endif
