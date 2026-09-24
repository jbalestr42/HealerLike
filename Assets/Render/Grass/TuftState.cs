using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // GPU layout of one tuft state, written by Grass.compute every frame
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Stride)]
    public struct TuftState
    {
        // The struct layout attribute needs a compile-time size
        public const int Stride = 16;

        // xy lean in radians, z height scale, w spike
        public Vector4 leanHeightSpike;
    }
}
