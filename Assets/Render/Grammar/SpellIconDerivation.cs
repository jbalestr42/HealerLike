using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class SpellIconDescription
    {
        public readonly List<ABuffHandlerFactory> handlers = new List<ABuffHandlerFactory>();
        public readonly List<EffectChannels> layers = new List<EffectChannels>();
        public EffectContext context = EffectContext.Default;
        public bool isSameSide = true;
    }

    // The only icon boundary that reads gameplay skill types. Names never determine a spell's symbol.
    public static class SpellIconDerivation
    {
        public static object Source(object source)
        {
            if (source is ABuffHandlerFactory || source is AConsumerFactory)
            {
                return source;
            }
            if (source is ACharacterSkill skill)
            {
                return skill.GetData();
            }
            return source is IGameDataSource owner ? owner.sourceData : source;
        }

        public static SpellIconDescription Read(object source)
        {
            source = Source(source);
            if (source == null || source is Object asset && !asset)
            {
                return null;
            }
            SpellIconDescription description = new SpellIconDescription();
            if (source is ABuffHandlerFactory handler)
            {
                description.handlers.Add(handler);
                return description;
            }
            if (source is AConsumerFactory consumer)
            {
                AddConsumer(description, consumer, 1f);
                return description;
            }
            if (!(source is BaseCharacterSkillData data))
            {
                return null;
            }
            description.context = new EffectContext
            {
                origin = EffectOrigin.Healer,
                targetCount = data.isSingle ? 1 : int.MaxValue
            };
            description.isSameSide = data.entityType == Entity.EntityType.Player;
            if (data is ApplyConsumerCharacterSkillData apply && apply.consumer)
            {
                AddConsumer(description, apply.consumer, apply.multiplier);
            }
            else if (data is BuffCharacterSkillData buff && buff.buffHandlerFactory != null)
            {
                foreach (ABuffHandlerFactory entry in buff.buffHandlerFactory)
                {
                    if (entry)
                    {
                        description.handlers.Add(entry);
                    }
                }
            }
            return description.layers.Count + description.handlers.Count > 0 ? description : null;
        }

        static void AddConsumer(SpellIconDescription description, AConsumerFactory consumer, float multiplier)
        {
            float harm = EffectDerivation.Harm(consumer) * multiplier;
            float amount = Mathf.Abs(harm) / LookDerivation.DefaultHealth;
            description.layers.Add(new EffectChannels
            {
                operation = harm > 0f ? EffectOperation.Damage : EffectOperation.Heal,
                family = harm > 0f ? EffectFamily.Damage : EffectFamily.Heal,
                magnitude = amount <= EffectDerivation.LightMagnitudeMax ? EffectMagnitude.Light
                    : amount <= EffectDerivation.SolidMagnitudeMax ? EffectMagnitude.Solid : EffectMagnitude.Heavy,
                tempo = EffectTempo.Once,
                reach = EffectDerivation.Reach(null, description.context.targetCount),
                delivery = EffectDelivery.Instant,
                trigger = EffectTrigger.Cast,
                origin = description.context.origin,
                side = description.isSameSide ? EffectSide.Ally : EffectSide.Opposing
            });
        }
    }
}
