using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // GPU layout of one blade state, written by HLGrass.compute every frame
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Stride)]
    public struct HLBladeState
    {
        // The struct layout attribute needs a compile-time size
        public const int Stride = 32;

        public Vector4 leanHeightSpike;
        public Vector4 rampHealReserved;
    }
}
