using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public enum DeliveryPathKind { Projectile, Chain, Beam }

    public struct DeliveryChannels
    {
        public DeliveryStyle style;
        public DeliveryPathKind path;
        public EffectFamily family;
        public bool bouncing;
        public bool splash;
    }

    // Only spell behaviour and payload select delivery presentation. Creature anatomy supplies no style.
    public static class DeliveryDerivation
    {
        public static DeliveryChannels Read(Projectile projectile, DeliveryStyle style)
        {
            DeliveryChannels channels = new DeliveryChannels { style = style, family = EffectFamily.Damage };
            if (!projectile)
            {
                return channels;
            }
            channels.bouncing = projectile.GetComponent<BounceProjectileBehaviour>() != null;
            channels.splash = projectile.GetComponent<AreaOfEffectProjectileBehaviour>() != null;
            ChainLightningProjectile chain = projectile as ChainLightningProjectile;
            if (chain || style == DeliveryStyle.ChainSync)
            {
                channels.style = DeliveryStyle.ChainSync;
                channels.path = chain && ChainProjectileReading.IsHeld(chain)
                    ? DeliveryPathKind.Beam : DeliveryPathKind.Chain;
            }
            else if (channels.bouncing && style != DeliveryStyle.Thrown)
            {
                channels.style = DeliveryStyle.Bounce;
            }
            channels.family = Family(projectile.onHitConsumers);
            return channels;
        }

        public static EffectFamily Family(IReadOnlyList<AConsumerFactory> consumers)
        {
            if (consumers != null)
            {
                foreach (AConsumerFactory consumer in consumers)
                {
                    if (consumer)
                    {
                        return EffectDerivation.ConsumerFamily(consumer, false);
                    }
                }
            }
            return EffectFamily.Damage;
        }
    }
}
