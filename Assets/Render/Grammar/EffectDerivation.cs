using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Reads the family, tempo and delivery of buffs and projectiles from their data, never from a name
    public static class EffectDerivation
    {
        // A curved homing prefab bent at least this much flies as a swarm, SwarmBullet is 3 and the others 1
        public static readonly float SwarmCurve = 2f;

        public static EffectChannels Channels(ABuffHandlerFactory handler, bool isSameSide)
        {
            EffectChannels channels = new EffectChannels();
            channels.family = Family(handler, isSameSide);
            channels.group = Group(handler);
            channels.tempo = Tempo(handler);
            channels.periodSeconds = Period(handler);
            return channels;
        }

        public static EffectFamily Family(ABuffHandlerFactory handler, bool isSameSide)
        {
            EffectFamily sideFamily = isSameSide ? EffectFamily.Boon : EffectFamily.Bane;
            IReadOnlyList<ABuffFactory> buffs = Buffs(handler);
            if (buffs.Count == 0)
            {
                return sideFamily;
            }

            // A heal is negative damage, so the consumer sign decides before any modifier
            foreach (ABuffFactory buff in buffs)
            {
                AConsumerFactory consumer = Consumer(buff);
                if (consumer != null)
                {
                    return ConsumerFamily(consumer, IsPeriodic(handler));
                }
            }

            int goodness = 0;
            foreach (ABuffFactory buff in buffs)
            {
                if (TryModifier(buff, out AttributeType type, out float delta) && delta != 0f)
                {
                    if (delta * Polarity(type) > 0f)
                    {
                        goodness++;
                    }
                    else
                    {
                        goodness--;
                    }
                }
            }

            if (goodness > 0)
            {
                return EffectFamily.Boon;
            }

            if (goodness < 0)
            {
                return EffectFamily.Bane;
            }
            return sideFamily;
        }

        public static EffectFamily ConsumerFamily(AConsumerFactory consumer, bool isPeriodic)
        {
            bool isHarm = Harm(consumer) > 0f;
            if (isPeriodic)
            {
                if (isHarm)
                {
                    return EffectFamily.Rot;
                }
                return EffectFamily.Renew;
            }

            if (isHarm)
            {
                return EffectFamily.Damage;
            }
            return EffectFamily.Heal;
        }

        // The sign of what the consumer takes from its target, positive for damage
        public static float Harm(AConsumerFactory consumer)
        {
            ConsumerFactory factory = consumer as ConsumerFactory;
            if (consumer != null && factory == null)
            {
                Debug.LogError($"[EffectDerivation] No harm reading for {consumer.GetType().Name}");
            }

            if (factory == null || factory.data == null || factory.data.value == null)
            {
                return 0f;
            }

            return SkillWalker.Value(factory.data.value, null);
        }

        // AttackRate is read as seconds between shots, so less is better
        public static float Polarity(AttributeType type)
        {
            if (type == AttributeType.AttackRate || type == AttributeType.Vulnerability)
            {
                return -1f;
            }
            return 1f;
        }

        public static AttributeGroup Group(ABuffHandlerFactory handler)
        {
            IReadOnlyList<ABuffFactory> buffs = Buffs(handler);
            if (buffs.Count == 0)
            {
                return AttributeGroup.Offence;
            }

            foreach (ABuffFactory buff in buffs)
            {
                if (buff is InvincibilityBuffFactory)
                {
                    return AttributeGroup.Prevention;
                }

                if (TryModifier(buff, out AttributeType type, out float delta))
                {
                    if (IsDefence(type))
                    {
                        return AttributeGroup.Defence;
                    }
                    return AttributeGroup.Offence;
                }
            }
            return AttributeGroup.Offence;
        }

        public static EffectTempo Tempo(ABuffHandlerFactory handler)
        {
            if (!HasData(handler) || handler.durationType == DurationType.Instant)
            {
                return EffectTempo.Once;
            }
            if (IsPeriodic(handler))
            {
                return EffectTempo.PerPeriod;
            }
            return EffectTempo.ForDuration;
        }

        public static float Period(ABuffHandlerFactory handler)
        {
            BuffHandlerFactory factory = handler as BuffHandlerFactory;
            if (factory == null || !IsPeriodic(handler))
            {
                return 0f;
            }
            return factory.data.periodDuration;
        }

        // Projectiles take the same reading as the head, so a unit's head and its shot agree
        public static DeliveryStyle Delivery(GameObject projectilePrefab)
        {
            return ProjectileDescriptionReader.Read(projectilePrefab).delivery;
        }

        public static bool IsPeriodic(ABuffHandlerFactory handler)
        {
            if (!HasData(handler) || handler.durationType == DurationType.Instant)
            {
                return false;
            }

            BuffHandlerFactory factory = handler as BuffHandlerFactory;
            return factory != null && factory.data.isPeriodic;
        }

        // The consumer a buff applies, for the buffs that apply one
        public static AConsumerFactory Consumer(ABuffFactory buff)
        {
            if (buff is ApplyConsumerBuffFactory applyConsumer && applyConsumer.data != null)
            {
                return applyConsumer.data.consumerFactory;
            }

            if (buff is DamageAllEntityOnEntityDieBuffFactory damageAll && damageAll.data != null)
            {
                return damageAll.data.damageToAllEntity;
            }

            if (buff is HealAllEntitiesOnRoundEndBuffFactory healAll && healAll.data != null)
            {
                return healAll.data.consumerFactory;
            }

            if (buff is ManaOnRoundEndBuffFactory mana && mana.data != null)
            {
                return mana.data.consumerFactory;
            }
            return null;
        }

        // The attribute a modifier buff changes and the sign of the change, Override counts as no change
        public static bool TryModifier(ABuffFactory buff, out AttributeType type, out float delta)
        {
            BaseData data = null;
            delta = 0f;
            if (buff is FlatModifierFactory flat && flat.data != null)
            {
                data = flat.data;
                delta = flat.data.value;
            }
            else if (buff is UpgradeModifierFactory upgrade && upgrade.data != null)
            {
                data = upgrade.data;
                delta = upgrade.data.value;
            }
            else if (buff is SlowModifierFactory slow && slow.data != null)
            {
                data = slow.data;
                delta = slow.data.value;
            }
            else if (buff is TimeModifierFactory time && time.data != null)
            {
                data = time.data;
                delta = time.data.value;
            }
            else if (buff is CurrentWaveModifierFactory wave && wave.data != null)
            {
                data = wave.data;
                delta = wave.data.value;
            }
            else if (buff is HPBasedModifierFactory hpBased && hpBased.data != null)
            {
                data = hpBased.data;
                delta = hpBased.data.factor;
            }

            type = data != null ? data.type : AttributeType.HealthMax;
            if (data == null || data.modifierType == AttributeModifierType.Override)
            {
                delta = 0f;
                return data != null;
            }
            return true;
        }

        // A handler made in memory can come without its data, its members would then throw
        static bool HasData(ABuffHandlerFactory handler)
        {
            BuffHandlerFactory factory = handler as BuffHandlerFactory;
            return handler != null && (factory == null || factory.data != null);
        }

        // A partially authored handler contributes no buffs until its data is assigned.
        public static IReadOnlyList<ABuffFactory> Buffs(ABuffHandlerFactory handler)
        {
            if (!HasData(handler) || handler.buffFactoryList == null)
            {
                return System.Array.Empty<ABuffFactory>();
            }
            return handler.buffFactoryList;
        }

        public static float Duration(ABuffHandlerFactory handler)
        {
            return HasData(handler) ? handler.duration : 0f;
        }

        static bool IsDefence(AttributeType type)
        {
            switch (type)
            {
                case AttributeType.HealthMax:
                case AttributeType.FlatArmor:
                case AttributeType.PercentArmor:
                case AttributeType.HitArmor:
                case AttributeType.Vulnerability:
                case AttributeType.CriticalChanceResist:
                    return true;
                default:
                    return false;
            }
        }
    }
}
