using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // A projectile's baked behaviours determine both its delivery and the head that announces it.
    public static class ProjectileDescriptionReader
    {
        public static ProjectileDescription Read(GameObject prefab)
        {
            ProjectileDescription description = new ProjectileDescription();
            if (prefab == null)
            {
                return description;
            }

            BounceProjectileBehaviour bounce = prefab.GetComponent<BounceProjectileBehaviour>();
            if (bounce != null && bounce.data != null)
            {
                description.bounces = bounce.data.bounce;
            }
            description.hasSplash = prefab.GetComponent<AreaOfEffectProjectileBehaviour>() != null;

            ChainLightningProjectile chain = prefab.GetComponent<ChainLightningProjectile>();
            CurvedHomingProjectileBehaviour curved = prefab.GetComponent<CurvedHomingProjectileBehaviour>();
            if (chain != null)
            {
                description.head = ChainProjectileReading.IsHeld(chain) ? HeadKind.Fork : HeadKind.Conductor;
                description.delivery = DeliveryStyle.ChainSync;
            }
            else if (curved != null || prefab.GetComponent<ArcHomingProjectileBehaviour>() != null)
            {
                description.head = HeadKind.Arch;
                description.delivery = DeliveryStyle.Arc;
            }
            else
            {
                HomingProjectileBehaviour homing = prefab.GetComponent<HomingProjectileBehaviour>();
                if (homing != null && homing.data != null && homing.data.speed >= LookDerivation.SpearSpeed)
                {
                    description.head = HeadKind.Spear;
                    description.delivery = DeliveryStyle.Rigid;
                }
            }

            if (curved != null && curved.data != null && curved.data.curveMultiplier >= EffectDerivation.SwarmCurve)
            {
                description.delivery = DeliveryStyle.Swarm;
            }
            return description;
        }
    }
}
