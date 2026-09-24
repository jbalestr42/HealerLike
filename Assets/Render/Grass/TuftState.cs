using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // GPU layout of one tuft state, written by Grass.compute every frame
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TuftState
    {
        // Bytes per element of its compute buffer, the marshalled size of the fields below
        public static readonly int Stride = 16;

        // xy lean in radians, z height scale, w spike
        public Vector4 leanHeightSpike;
    }
}
