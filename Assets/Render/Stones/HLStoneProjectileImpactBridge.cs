using UnityEngine;

namespace HealerLike.Render.Stones
{
    [RequireComponent(typeof(Projectile))]
    public class HLStoneProjectileImpactBridge : MonoBehaviour
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

            GameObject target = data.attackable?.owner;
            if (target == null)
            {
                target = data.target;
            }
            HLStoneEnemyVisual visual = target != null ? target.GetComponentInChildren<HLStoneEnemyVisual>() : null;
            if (visual == null)
            {
                return;
            }

            // Projectile.target is already cleared by ApplyOnHit; the callback data keeps the target.
            Vector3 direction = transform.forward;
            if (data.source != null)
            {
                direction = (transform.position - data.source.transform.position).normalized;
            }
            visual.RecordImpact(data.resourceModifier, visual.EstimateImpact(transform.position, direction));
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
