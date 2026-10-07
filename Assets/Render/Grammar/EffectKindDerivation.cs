using System.Collections.Generic;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        // The first buff whose factory has a kind decides the handler's kind
        public static EffectKind Kind(ABuffHandlerFactory handler)
        {
            foreach (ABuffFactory buff in Buffs(handler))
            {
                EffectKind kind = Kind(buff);
                if (kind != EffectKind.Plain)
                {
                    return kind;
                }
            }
            return EffectKind.Plain;
        }

        // A handler a growing item stacks on its holder draws its growth, whatever modifier builds it
        public static EffectKind Kind(ABuffHandlerFactory handler, EffectContext context)
        {
            return IsGrowth(handler, context) ? EffectKind.Growth : Kind(handler);
        }

        public static bool IsGrowth(ABuffHandlerFactory handler, EffectContext context)
        {
            return handler != null && context.growthHandlers != null && context.growthHandlers.Contains(handler);
        }

        // The handlers these items stack as they grow, each battle and each kill
        public static HashSet<ABuffHandlerFactory> GrowthHandlers(IEnumerable<object> itemData)
        {
            HashSet<ABuffHandlerFactory> handlers = new HashSet<ABuffHandlerFactory>();
            if (itemData == null)
            {
                return handlers;
            }

            foreach (object data in itemData)
            {
                if (!(data is GrowingItemData growing))
                {
                    continue;
                }

                if (growing.growthBuffHandlerFactory)
                {
                    handlers.Add(growing.growthBuffHandlerFactory);
                }

                if (growing.killGrowthBuffHandlerFactory)
                {
                    handlers.Add(growing.killGrowthBuffHandlerFactory);
                }
            }
            return handlers;
        }

        public static EffectKind Kind(ABuffFactory buff)
        {
            if (buff is ProjectileBehaviourBuffFactory)
            {
                return EffectKind.Projectile;
            }

            if (buff is MultipleShootBuffFactory)
            {
                return EffectKind.Volume;
            }

            if (buff is TimeModifierFactory)
            {
                return EffectKind.Rate;
            }

            if (buff is HPBasedModifierFactory || buff is HealthThresholdModifierFactory
                || buff is ApplyBuffBelowHealthBuffFactory)
            {
                return EffectKind.Conditional;
            }

            if (buff is BoostEntitiesOnRelativeCellBuffFactory || buff is ShareHealOnRelativeCellBuffFactory
                || buff is AlliesOnRelativeCellModifierFactory)
            {
                return EffectKind.Positional;
            }

            if (buff is ApplyBuffOnEventBuffFactory || buff is ConsumerOnAttackBuffFactory
                || buff is DamageEnemyOnHealBuffFactory || buff is EmpowerNextAttackOnHealBuffFactory
                || buff is ManaOnKillBuffFactory || buff is ManaOnOverhealBuffFactory)
            {
                return EffectKind.Reactive;
            }

            if (buff is EchoAttackBuffFactory)
            {
                return EffectKind.Echo;
            }

            if (buff is SoulLinkBuffFactory)
            {
                return EffectKind.Link;
            }

            if (buff is ReviveOnDeathBuffFactory || buff is SummonOnKillBuffFactory)
            {
                return EffectKind.Summon;
            }

            if (buff is FlatModifierFactory)
            {
                return EffectKind.Flat;
            }

            return EffectKind.Plain;
        }
    }
}
