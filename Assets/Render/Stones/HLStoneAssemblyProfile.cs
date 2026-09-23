using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    [CreateAssetMenu(menuName = "HealerLike/Stone assembly")]
    public class HLStoneAssemblyProfile : ScriptableObject
    {
        [FormerlySerializedAs("Parts")]
        public HLStonePart[] parts;
        [FormerlySerializedAs("DetachablePartIndex")]
        public int detachablePartIndex = 2;
        [FormerlySerializedAs("ShedHealthFraction")]
        public float shedHealthFraction = 0.5f;
    }
}
