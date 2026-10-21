using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Seeded stone meshes baked offline, a Stone part picks one by its variant
    [CreateAssetMenu(menuName = "Custom/Data/Render/StoneVariants")]
    public class StoneVariants : ScriptableObject
    {
        public Mesh[] meshes = new Mesh[0];
    }
}
