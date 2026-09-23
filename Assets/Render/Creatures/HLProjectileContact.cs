using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLProjectileContact
    {
        public readonly GameObject target;
        public readonly Vector3 position;

        public HLProjectileContact(GameObject target, Vector3 position)
        {
            this.target = target;
            this.position = position;
        }
    }
}
