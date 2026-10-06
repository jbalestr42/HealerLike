using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        public static EffectChannels Channels(ABuffHandlerFactory handler, bool isSameSide)
        {
            return Channels(handler, isSameSide, EffectContext.Default);
        }

        public static EffectChannels Channels(ABuffHandlerFactory handler, bool isSameSide, EffectContext context)
        {
            return new EffectChannels {
                family = Family(handler, isSameSide), group = Group(handler),
                operation = Operation(handler, isSameSide), aspect = Aspect(handler),
                tempo = Tempo(handler), periodSeconds = Period(handler), magnitude = Magnitude(handler, context),
                reach = Reach(handler, context.targetCount), delivery = DeliveryChannel(handler, context), trigger = Trigger(handler, context),
                side = isSameSide ? EffectSide.Ally : EffectSide.Opposing, origin = context.origin, material = context.material,
                kind = Kind(handler, context)
            };
        }

        public static EffectOperation Operation(ABuffHandlerFactory handler, bool isSameSide = true)
        {
            foreach (ABuffFactory buff in Buffs(handler))
            {
                if (buff is InvincibilityBuffFactory) return EffectOperation.Ward;
                // Mana restored to the healer: each round, each kill or from an overheal
                if (buff is ManaOnRoundEndBuffFactory || buff is ManaOnKillBuffFactory
                    || buff is ManaOnOverhealBuffFactory) return EffectOperation.Mana;
                if (buff is DrainCharacterManaBuffFactory) return EffectOperation.ManaDrain;
            }
            switch (Family(handler, isSameSide))
            {
                case EffectFamily.Heal:
                case EffectFamily.Renew: return EffectOperation.Heal;
                case EffectFamily.Boon: return EffectOperation.Boon;
                case EffectFamily.Bane: return EffectOperation.Bane;
                default: return EffectOperation.Damage;
            }
        }

        public static EffectAspect Aspect(ABuffHandlerFactory handler) { return (EffectAspect)Group(handler); }

        public static readonly float LightMagnitudeMax = 0.1f;
        public static readonly float SolidMagnitudeMax = 0.4f;

        public static EffectMagnitude Magnitude(ABuffHandlerFactory handler)
        {
            return Magnitude(handler, EffectContext.Default);
        }

        public static EffectMagnitude Magnitude(ABuffHandlerFactory handler, EffectContext context)
        {
            float reference = 0f;
            foreach (ABuffFactory buff in Buffs(handler))
            {
                AConsumerFactory consumer = Consumer(buff);
                if (consumer != null)
                {
                    reference = Mathf.Max(reference, Mathf.Abs(Harm(consumer, context)) / HealthReference(context));
                }
                reference = Mathf.Max(reference, ModifierMagnitudeShare(buff, context));
            }
            return reference <= LightMagnitudeMax ? EffectMagnitude.Light
                : reference <= SolidMagnitudeMax ? EffectMagnitude.Solid : EffectMagnitude.Heavy;
        }

        public static EffectReach Reach(ABuffHandlerFactory handler, int targetCount = 1)
        {
            if (targetCount <= 1) return EffectReach.Single;
            return targetCount == int.MaxValue ? EffectReach.All : EffectReach.Group;
        }

        // A link has no projectile: what it delivers runs between the holder and its allies
        public static EffectDelivery DeliveryChannel(ABuffHandlerFactory handler, EffectContext context)
        {
            return Kind(handler) == EffectKind.Link ? EffectDelivery.Link : DeliveryChannel(context.projectilePrefab);
        }

        public static EffectDelivery DeliveryChannel(GameObject projectilePrefab = null)
        {
            if (projectilePrefab == null) return EffectDelivery.Instant;
            switch (Delivery(projectilePrefab))
            {
                case DeliveryStyle.Direct:
                case DeliveryStyle.Rigid: return EffectDelivery.Rigid;
                case DeliveryStyle.Arc: return EffectDelivery.Arc;
                case DeliveryStyle.Swarm: return EffectDelivery.Swarm;
                case DeliveryStyle.ChainSync: return EffectDelivery.ChainSync;
                default: return EffectDelivery.Instant;
            }
        }

        public static EffectTrigger Trigger(ABuffHandlerFactory handler)
        {
            return Trigger(handler, EffectContext.Default);
        }

        public static EffectTrigger Trigger(ABuffHandlerFactory handler, EffectContext context)
        {
            if (context.triggers != null && context.triggers.Length > 0)
            {
                return context.triggers[0];
            }
            foreach (ABuffFactory buff in Buffs(handler))
            {
                if (buff is DamageAllEntityOnEntityDieBuffFactory) return EffectTrigger.OnDeath;
                if (buff is HealAllEntitiesOnRoundEndBuffFactory || buff is ManaOnRoundEndBuffFactory)
                    return EffectTrigger.RoundEnd;
            }
            return EffectTrigger.Cast;
        }

        public static EffectOrigin Origin(ABuffHandlerFactory handler = null) { return EffectOrigin.Creature; }

        public static EffectOrigin Origin(GameObject source)
        {
            if (source == null) return EffectOrigin.Creature;
            if (source.GetComponent<Character>() != null) return EffectOrigin.Healer;
            if (source.GetComponent<Entity>() != null) return EffectOrigin.Creature;
            return EffectOrigin.Item;
        }
    }
}
