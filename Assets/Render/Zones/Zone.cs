using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // What the tuft compute and the heal ring read from the zone buffer: heal lift and ring, hostile spikes, a
    // range preview's lift and a bruise's sink. The values are read by the shaders, do not renumber them.
    public enum ZoneKind
    {
        None = 0,
        Heal = 1,
        Hostile = 2,
        Range = 3,
        // Everything else the grass shows goes through Ground, not the zones
        Bruise = 4
    }

    // Matches the 32 bytes element of _HLZones in ZoneData.hlsl
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct Zone
    {
        [FieldOffset(0)] public Vector3 position;
        [FieldOffset(12)] public float radius;
        [FieldOffset(16)] public int kind;
        [FieldOffset(20)] public float strength;
        [FieldOffset(24)] public float age;
        // Unused, zero; it keeps the element at the 32 bytes the shaders read
        [FieldOffset(28)] public uint reserved;

        public static readonly int Stride = 32;
    }
}
