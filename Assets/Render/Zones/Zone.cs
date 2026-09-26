using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // The values are read by the shaders, do not renumber them
    public enum ZoneKind
    {
        None = 0,
        Heal = 1,
        Hostile = 2,
        Range = 3,
        Bruise = 4,
        Launch = 5,
        Trample = 6,
        // A blast ring: an impact throwing the grass outward
        Shock = 7,
        // Grass burnt to ash around a rocky enemy, shrinking as its health falls
        Ash = 8,
        // Grass dying around an ally, spreading as its health falls
        Wilt = 9
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
        // Launch: XZ heading encoded as uint turns, zero for the other kinds
        [FieldOffset(28)] public uint reserved;

        public static readonly int Stride = 32;
    }
}
