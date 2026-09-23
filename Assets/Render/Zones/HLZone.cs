using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // The values are read by the shaders, do not renumber them
    public enum HLZoneKind
    {
        None = 0,
        Heal = 1,
        Hostile = 2,
        Range = 3,
        Bruise = 4,
        Launch = 5,
        Trample = 6
    }

    // Matches the 32 bytes element of _HL_Zones in HLZoneData.hlsl
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct HLZone
    {
        [FieldOffset(0)] public Vector3 position;
        [FieldOffset(12)] public float radius;
        [FieldOffset(16)] public int kind;
        [FieldOffset(20)] public float strength;
        [FieldOffset(24)] public float age;
        // Launch: XZ heading encoded as uint turns, zero for the other kinds
        [FieldOffset(28)] public uint reserved;

        public static readonly int Stride = 32;
    }
}
