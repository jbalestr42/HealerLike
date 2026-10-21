using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // GPU layout of one tuft seed, read by Grass.compute and GrassInstancing.hlsl
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TuftSeed
    {
        // Bytes per element of its compute buffer, the marshalled size of the fields below
        public static readonly int Stride = 32;

        public Vector4 positionYaw;
        // x height, y width, zw rest lean: the tilt toward that heading, its length in radians
        public Vector4 heightWidthLean;
    }
}
