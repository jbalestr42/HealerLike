using System;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    [Serializable]
    public struct StoneSettings
    {
        [FormerlySerializedAs("Size")]
        public float size;
        [FormerlySerializedAs("Elongation")]
        public float elongation;
        [FormerlySerializedAs("DepthRatio")]
        public float depthRatio;
        [FormerlySerializedAs("Roughness")]
        public float roughness;
        [FormerlySerializedAs("Subdivisions")]
        public int subdivisions;
    }
}
