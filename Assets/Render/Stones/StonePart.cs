using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    [Serializable]
    public struct StonePart
    {
        [FormerlySerializedAs("Shape")]
        public StoneSettings shape;
        [FormerlySerializedAs("LocalPosition")]
        public Vector3 localPosition;
        [FormerlySerializedAs("LocalEulerAngles")]
        public Vector3 localEulerAngles;
        [FormerlySerializedAs("SeedSalt")]
        public uint seedSalt;
        [FormerlySerializedAs("PaletteIndex")]
        public int paletteIndex;
    }
}
