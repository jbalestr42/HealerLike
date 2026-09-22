using System;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    [Serializable]
    public struct HLStonePart
    {
        public HLStoneSettings Shape;
        public Vector3 LocalPosition, LocalEulerAngles;
        public uint SeedSalt;
        public int PaletteIndex;
    }
    [CreateAssetMenu(menuName="HealerLike/Stone assembly")]
    public sealed class HLStoneAssemblyProfile : ScriptableObject
    {
        public HLStonePart[] Parts;
        public int DetachablePartIndex=2;
        public float ShedHealthFraction=.5f;
    }
}
