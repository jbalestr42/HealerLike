using UnityEngine;

namespace HealerLike.Render.Stones
{
    [RequireComponent(typeof(Projectile))]
    public class StoneProjectileImpactBridge : MonoBehaviour
    {
        Projectile _projectile;

        void OnEnable()
        {
            _projectile = GetComponent<Projectile>();
            _projectile.OnHit.AddListener(OnHit);
        }

        void OnHit(OnHitData data)
        {
            if (data == null)
            {
                return;
            }

            GameObject target = null;
            if (data.attackable != null)
            {
                target = data.attackable.owner;
            }
            if (target == null)
            {
                target = data.target;
            }
            StoneBody body = target != null ? target.GetComponentInChildren<StoneBody>() : null;
            if (body == null)
            {
                return;
            }

            // Projectile.target is already cleared by ApplyOnHit; the callback data keeps the target.
            Vector3 direction = transform.forward;
            if (data.source != null)
            {
                direction = (transform.position - data.source.transform.position).normalized;
            }
            body.RecordImpact(data.resourceModifier, body.EstimateImpact(transform.position, direction));
        }

        void OnDisable()
        {
            if (_projectile != null)
            {
                _projectile.OnHit.RemoveListener(OnHit);
            }
        }
    }
}
