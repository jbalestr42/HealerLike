using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Reads the family, tempo and delivery of his buffs and projectiles from their data, never from a name
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
            if (!HasBuffs(handler))
            {
                return sideFamily;
            }

            // A heal is negative damage, so the consumer sign decides before any modifier
            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                AConsumerFactory consumer = Consumer(buff);
                if (consumer != null)
                {
                    return ConsumerFamily(consumer, IsPeriodic(handler));
                }
            }

            int goodness = 0;
            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (TryModifier(buff, out AttributeType type, out float delta) && delta != 0f)
                {
                    goodness += delta * Polarity(type) > 0f ? 1 : -1;
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
                return isHarm ? EffectFamily.Rot : EffectFamily.Renew;
            }
            return isHarm ? EffectFamily.Damage : EffectFamily.Heal;
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
            if (!HasBuffs(handler))
            {
                return AttributeGroup.Offence;
            }

            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (buff is InvincibilityBuffFactory)
                {
                    return AttributeGroup.Prevention;
                }

                if (TryModifier(buff, out AttributeType type, out float delta))
                {
                    return IsDefence(type) ? AttributeGroup.Defence : AttributeGroup.Offence;
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
            return IsPeriodic(handler) ? EffectTempo.PerPeriod : EffectTempo.ForDuration;
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
            if (projectilePrefab == null)
            {
                return DeliveryStyle.Direct;
            }

            CurvedHomingProjectileBehaviour curved = projectilePrefab.GetComponent<CurvedHomingProjectileBehaviour>();
            if (curved != null && curved.data != null && curved.data.curveMultiplier >= SwarmCurve)
            {
                return DeliveryStyle.Swarm;
            }

            switch (LookDerivation.Delivery(projectilePrefab))
            {
                case HeadKind.Conductor:
                case HeadKind.Fork:
                    return DeliveryStyle.ChainSync;
                case HeadKind.Arch:
                    return DeliveryStyle.Arc;
                case HeadKind.Spear:
                    return DeliveryStyle.Rigid;
                default:
                    return DeliveryStyle.Direct;
            }
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
            if (buff is ApplyConsumerBuffFactory applyConsumer)
            {
                return applyConsumer.data.consumerFactory;
            }

            if (buff is DamageAllEntityOnEntityDieBuffFactory damageAll)
            {
                return damageAll.data.damageToAllEntity;
            }

            if (buff is HealAllEntitiesOnRoundEndBuffFactory healAll)
            {
                return healAll.data.consumerFactory;
            }

            if (buff is ManaOnRoundEndBuffFactory mana)
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
            if (buff is FlatModifierFactory flat)
            {
                data = flat.data;
                delta = flat.data.value;
            }
            else if (buff is UpgradeModifierFactory upgrade)
            {
                data = upgrade.data;
                delta = upgrade.data.value;
            }
            else if (buff is SlowModifierFactory slow)
            {
                data = slow.data;
                delta = slow.data.value;
            }
            else if (buff is TimeModifierFactory time)
            {
                data = time.data;
                delta = time.data.value;
            }
            else if (buff is CurrentWaveModifierFactory wave)
            {
                data = wave.data;
                delta = wave.data.value;
            }
            else if (buff is HPBasedModifierFactory hpBased)
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

        static bool HasBuffs(ABuffHandlerFactory handler)
        {
            return HasData(handler) && handler.buffFactoryList != null;
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
