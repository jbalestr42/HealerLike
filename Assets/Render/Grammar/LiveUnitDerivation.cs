using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Pure translation of live simulation values into creature grammar; observers only manage subscriptions.
    public static class LiveUnitDerivation
    {
        public static readonly float MinimumChange = 0.001f;
        public static readonly float RelativeChange = 0.02f;
        public static readonly IReadOnlyList<AttributeType> observedAttributes = System.Array.AsReadOnly(new[]
        {
            AttributeType.HealthMax, AttributeType.AttackRate, AttributeType.Range,
            AttributeType.Damage, AttributeType.HealPower, AttributeType.FlatArmor,
            AttributeType.PercentArmor, AttributeType.HitArmor, AttributeType.Speed,
            AttributeType.CriticalChance, AttributeType.CriticalMultiplier, AttributeType.CriticalChanceResist,
            AttributeType.Vulnerability
        });
        public static UnitChannels Read(EntityData data, Entity.EntityType side,
            IReadOnlyDictionary<AttributeType, Attribute> attributes)
        {
            UnitChannels channels = LookDerivation.Channels(data, side);
            if (TryValue(attributes, AttributeType.HealthMax, out float health))
            {
                channels.mass = LookDerivation.Mass(health);
            }

            if (TryValue(attributes, AttributeType.Range, out float range))
            {
                channels.reach = range <= LookDerivation.ShortRange ? ReachBand.Short
                    : range <= LookDerivation.MidRange ? ReachBand.Mid : ReachBand.Long;
            }

            ASkillFactory primary = LookDerivation.Primary(data);
            // Both runtime skills use AttackRate as seconds per trigger, despite the attribute's name.
            if ((primary is ShootProjectileSkillFactory || primary is AreaOfEffectSkillFactory)
                && TryValue(attributes, AttributeType.AttackRate, out float cadence))
            {
                channels.stem = LookDerivation.Stem(cadence);
            }

            // Preserve structural accessories such as a second delivery. Free slots express live stat changes.
            if (channels.accessory == AccessoryKind.None)
            {
                int direction = UpgradeDirection(attributes);
                if (direction != 0)
                {
                    channels.accessory = direction > 0 ? AccessoryKind.SmallTorus : AccessoryKind.ConeCrown;
                }
            }
            return channels;
        }

        static int UpgradeDirection(IReadOnlyDictionary<AttributeType, Attribute> attributes)
        {
            bool improved = false;
            foreach (AttributeType type in observedAttributes)
            {
                if (!attributes.TryGetValue(type, out Attribute attribute) || !float.IsFinite(attribute.Value)
                    || !float.IsFinite(attribute.BaseValue))
                {
                    continue;
                }

                float delta = attribute.Value - attribute.BaseValue;
                float threshold = Mathf.Max(MinimumChange, Mathf.Abs(attribute.BaseValue) * RelativeChange);
                if (Mathf.Abs(delta) <= threshold)
                {
                    continue;
                }

                delta *= EffectDerivation.Polarity(type);
                if (delta < 0f)
                {
                    return -1;
                }

                improved = true;
            }
            return improved ? 1 : 0;
        }

        static bool TryValue(IReadOnlyDictionary<AttributeType, Attribute> attributes, AttributeType type,
            out float value)
        {
            value = 0f;
            if (!attributes.TryGetValue(type, out Attribute attribute))
            {
                return false;
            }

            value = attribute.Value;
            return float.IsFinite(value) && value >= 0f;
        }

    }
}
