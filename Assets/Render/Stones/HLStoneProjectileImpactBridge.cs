using UnityEngine;
namespace HealerLike.Render.Stones
{
    [RequireComponent(typeof(Projectile))]
    public sealed class HLStoneProjectileImpactBridge : MonoBehaviour
    {
        Projectile projectile;
        void OnEnable() { projectile=GetComponent<Projectile>(); projectile.OnHit.AddListener(OnHit); }
        void OnHit(OnHitData data)
        {
            if(data==null) return;
            var target=data.attackable?.owner; if(target==null) target=data.target;
            var visual=target!=null?target.GetComponentInChildren<HLStoneEnemyVisual>():null;
            if(visual==null) return;
            // Projectile.target is already cleared by ApplyOnHit; callback data retains the target.
            Vector3 direction=data.source!=null?(transform.position-data.source.transform.position).normalized:transform.forward;
            visual.RecordImpact(data.resourceModifier,visual.EstimateImpact(transform.position,direction));
        }
        void OnDisable() { if(projectile!=null) projectile.OnHit.RemoveListener(OnHit); }
    }
}
