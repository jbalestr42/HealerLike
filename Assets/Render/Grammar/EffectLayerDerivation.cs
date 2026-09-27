using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        // A compound handler keeps every operation. In particular, beneficial and harmful modifiers never vote.
        public static IReadOnlyList<EffectChannels> Layers(ABuffHandlerFactory handler, bool isSameSide)
        {
            return Layers(handler, isSameSide, EffectContext.Default);
        }

        public static IReadOnlyList<EffectChannels> Layers(ABuffHandlerFactory handler, bool isSameSide,
            EffectContext context)
        {
            IReadOnlyList<ABuffFactory> buffs = Buffs(handler);
            if (buffs.Count == 0) return System.Array.Empty<EffectChannels>();
            var layers = new List<EffectChannels>(buffs.Count);
            foreach (ABuffFactory buff in buffs)
            {
                if (!buff) continue;
                EffectFamily family = LayerFamily(buff, isSameSide, IsPeriodic(handler));
                AttributeGroup group = LayerGroup(buff);
                layers.Add(new EffectChannels
                {
                    family = family,
                    group = group,
                    operation = LayerOperation(buff, family),
                    aspect = (EffectAspect)group,
                    magnitude = LayerMagnitude(buff),
                    tempo = Tempo(handler),
                    periodSeconds = Period(handler),
                    reach = Reach(handler, context.targetCount),
                    delivery = DeliveryChannel(context.projectilePrefab),
                    trigger = LayerTrigger(buff, context),
                    side = isSameSide ? EffectSide.Ally : EffectSide.Opposing,
                    origin = context.origin
                });
            }
            return layers;
        }

        static EffectFamily LayerFamily(ABuffFactory buff, bool isSameSide, bool periodic)
        {
            AConsumerFactory consumer = Consumer(buff);
            if (consumer != null) return ConsumerFamily(consumer, periodic);
            if (TryModifier(buff, out AttributeType type, out float delta) && delta != 0f)
                return delta * Polarity(type) > 0f ? EffectFamily.Boon : EffectFamily.Bane;
            return isSameSide ? EffectFamily.Boon : EffectFamily.Bane;
        }

        static AttributeGroup LayerGroup(ABuffFactory buff)
        {
            if (buff is InvincibilityBuffFactory) return AttributeGroup.Prevention;
            return TryModifier(buff, out AttributeType type, out _) && IsDefence(type)
                ? AttributeGroup.Defence : AttributeGroup.Offence;
        }

        static EffectOperation LayerOperation(ABuffFactory buff, EffectFamily family)
        {
            if (buff is InvincibilityBuffFactory) return EffectOperation.Ward;
            if (buff is ManaOnRoundEndBuffFactory) return EffectOperation.Mana;
            switch (family)
            {
                case EffectFamily.Heal:
                case EffectFamily.Renew: return EffectOperation.Heal;
                case EffectFamily.Boon: return EffectOperation.Boon;
                case EffectFamily.Bane: return EffectOperation.Bane;
                default: return EffectOperation.Damage;
            }
        }

        static EffectMagnitude LayerMagnitude(ABuffFactory buff)
        {
            AConsumerFactory consumer = Consumer(buff);
            float reference = consumer != null ? Mathf.Abs(Harm(consumer)) / LookDerivation.DefaultHealth : 0f;
            reference = Mathf.Max(reference, ModifierMagnitudeShare(buff));
            return reference <= LightMagnitudeMax ? EffectMagnitude.Light
                : reference <= SolidMagnitudeMax ? EffectMagnitude.Solid : EffectMagnitude.Heavy;
        }

        static EffectTrigger LayerTrigger(ABuffFactory buff, EffectContext context)
        {
            if (context.triggers != null && context.triggers.Length > 0) return context.triggers[0];
            if (buff is DamageAllEntityOnEntityDieBuffFactory) return EffectTrigger.OnDeath;
            if (buff is HealAllEntitiesOnRoundEndBuffFactory || buff is ManaOnRoundEndBuffFactory)
                return EffectTrigger.RoundEnd;
            return EffectTrigger.Cast;
        }
    }
}
