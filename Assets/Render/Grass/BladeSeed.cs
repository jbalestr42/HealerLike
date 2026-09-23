using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // GPU layout of one blade seed, read by Grass.compute and GrassInstancing.hlsl
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Stride)]
    public struct BladeSeed
    {
        // The struct layout attribute needs a compile-time size
        public const int Stride = 32;

        public Vector4 positionYaw;
        public Vector4 heightPhaseWidthRandom;
    }
}
