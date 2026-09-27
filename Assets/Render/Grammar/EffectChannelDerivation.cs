using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        public static EffectChannels Channels(ABuffHandlerFactory handler, bool isSameSide)
        {
            return new EffectChannels {
                family = Family(handler, isSameSide), group = Group(handler),
                operation = Operation(handler, isSameSide), aspect = Aspect(handler),
                tempo = Tempo(handler), periodSeconds = Period(handler), magnitude = Magnitude(handler),
                reach = Reach(handler), delivery = DeliveryChannel(), trigger = Trigger(handler),
                side = isSameSide ? EffectSide.Ally : EffectSide.Opposing, origin = EffectOrigin.Creature
            };
        }

        public static EffectOperation Operation(ABuffHandlerFactory handler, bool isSameSide = true)
        {
            foreach (ABuffFactory buff in Buffs(handler))
            {
                if (buff is InvincibilityBuffFactory) return EffectOperation.Ward;
                if (buff is ManaOnRoundEndBuffFactory) return EffectOperation.Mana;
            }
            return (EffectOperation)Family(handler, isSameSide);
        }

        public static EffectAspect Aspect(ABuffHandlerFactory handler) { return (EffectAspect)Group(handler); }

        public static EffectMagnitude Magnitude(ABuffHandlerFactory handler)
        {
            int count = Buffs(handler).Count;
            return count <= 1 ? EffectMagnitude.Light : count <= 3 ? EffectMagnitude.Solid : EffectMagnitude.Heavy;
        }

        public static EffectReach Reach(ABuffHandlerFactory handler, int targetCount = 1)
        {
            if (targetCount <= 1) return EffectReach.Single;
            return targetCount == int.MaxValue ? EffectReach.All : EffectReach.Group;
        }

        public static EffectDelivery DeliveryChannel(GameObject projectilePrefab = null)
        {
            if (projectilePrefab == null) return EffectDelivery.Instant;
            switch (Delivery(projectilePrefab))
            {
                case DeliveryStyle.Direct: return EffectDelivery.Rigid;
                case DeliveryStyle.Arc: return EffectDelivery.Arc;
                case DeliveryStyle.Swarm: return EffectDelivery.Swarm;
                case DeliveryStyle.ChainSync: return EffectDelivery.ChainSync;
                default: return EffectDelivery.Instant;
            }
        }

        public static EffectTrigger Trigger(ABuffHandlerFactory handler)
        {
            return handler != null && IsPeriodic(handler) ? EffectTrigger.RoundEnd : EffectTrigger.Cast;
        }

        public static EffectOrigin Origin(ABuffHandlerFactory handler = null) { return EffectOrigin.Creature; }
    }
}
