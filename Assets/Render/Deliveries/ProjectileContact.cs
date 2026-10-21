using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public readonly struct ProjectileContact
    {
        public readonly GameObject target;
        public readonly Vector3 position;

        public ProjectileContact(GameObject target, Vector3 position)
        {
            this.target = target;
            this.position = position;
        }
    }
}
