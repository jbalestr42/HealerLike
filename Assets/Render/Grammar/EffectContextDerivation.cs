using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        // Capture plain values at the gameplay boundary. Composers never inspect live attributes.
        public static EffectContext Context(GameObject source, GameObject target)
        {
            EffectContext context = EffectContext.Default;
            context.origin = Origin(source);
            Entity recipient = target ? target.GetComponent<Entity>() : null;
            // Entity publishes this reference during Init; a preview's required component may not have Awoken.
            AttributeManager attributes = recipient ? recipient.attributeManager : null;
            if (!attributes)
            {
                return context;
            }
            Dictionary<AttributeType, float> baselines = new Dictionary<AttributeType, float>();
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                if (attributes.Has(type))
                {
                    baselines[type] = attributes.Get(type).BaseValue;
                }
            }
            context.attributeBaselines = baselines;
            if (attributes.Has(AttributeType.HealthMax))
            {
                context.maximumHealth = attributes.Get(AttributeType.HealthMax).Value;
            }
            return context;
        }

        // The caster's side against the target's: the healer's Character, which is not an Entity, plays for the
        // player, and a status without a caster is taken as its target's own
        public static bool IsSameSide(GameObject source, GameObject target)
        {
            if (source == null || target == null)
            {
                return true;
            }

            Entity caster = source.GetComponent<Entity>();
            Entity recipient = target.GetComponent<Entity>();
            Entity.EntityType casterSide = Entity.EntityType.Player;
            if (caster != null)
            {
                casterSide = caster.entityType;
            }

            Entity.EntityType recipientSide = Entity.EntityType.Player;
            if (recipient != null)
            {
                recipientSide = recipient.entityType;
            }

            return casterSide == recipientSide;
        }

        public static float HealthReference(EffectContext context)
        {
            return float.IsFinite(context.maximumHealth) && context.maximumHealth > 0f
                ? context.maximumHealth : LookDerivation.DefaultHealth;
        }

        static float AttributeReference(EffectContext context, AttributeType type)
        {
            if (context.attributeBaselines != null && context.attributeBaselines.TryGetValue(type, out float value)
                && float.IsFinite(value) && Mathf.Abs(value) > 0f)
            {
                return Mathf.Abs(value);
            }
            return LookDerivation.DefaultHealth;
        }
    }
}
