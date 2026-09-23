using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    [CreateAssetMenu(menuName = "Custom/Data/Render/StoneAssemblyProfile")]
    public class StoneAssemblyProfile : ScriptableObject
    {
        [FormerlySerializedAs("Parts")]
        public StonePart[] parts;
        [FormerlySerializedAs("DetachablePartIndex")]
        public int detachablePartIndex = 2;
        [FormerlySerializedAs("ShedHealthFraction")]
        public float shedHealthFraction = 0.5f;
    }
}
