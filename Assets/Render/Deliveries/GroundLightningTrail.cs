using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // A chain's first contact starts at the projectile; later contacts continue from its previous target.
    public class GroundLightningTrail
    {
        Vector3 _end;
        bool _hasContact;

        public void Contact(Ground ground, Vector3 source, GameObject target)
        {
            if (ground == null || !target)
            {
                return;
            }
            Vector3 to = target.transform.position;
            ground.Play(ground.vocabulary.scorch, _hasContact ? _end : source, to);
            _end = to;
            _hasContact = true;
        }

        public void Clear()
        {
            _hasContact = false;
        }
    }
}
