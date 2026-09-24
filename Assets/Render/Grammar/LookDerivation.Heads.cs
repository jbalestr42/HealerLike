using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // The silhouette half of the reading: the head, the accessory and the accent the skills draw
    public static partial class LookDerivation
    {
        public static HeadKind Head(ASkillFactory skill)
        {
            if (skill is ShootProjectileSkillFactory || skill is ConfigurableSkillFactory)
            {
                return Delivery(SkillWalker.DominantPrefab(skill));
            }

            if (skill is ApplyBuffOnTargetSkillFactory support)
            {
                ABuffHandlerFactory handler = support.data.buffHandlerFactory;
                return Gift(EffectDerivation.Family(handler, support.data.targetAlly), EffectDerivation.Group(handler));
            }

            if (skill is HealTargetSkillFactory)
            {
                return HeadKind.GiftHeal;
            }

            if (skill is AreaOfEffectSkillFactory)
            {
                return HeadKind.Pulse;
            }

            if (skill is ApplyConsumerOnTimeFactory)
            {
                return HeadKind.SelfTick;
            }

            if (skill is ApplyBuffPeriodicallySkillFactory periodic && periodic.data.periodicBuff != null
                && periodic.data.periodicBuff.Count > 0)
            {
                if (IsEveryBoon(periodic.data.periodicBuff))
                {
                    return HeadKind.Ward;
                }

                ABuffHandlerFactory first = periodic.data.periodicBuff[0];
                return Gift(EffectDerivation.Family(first, true), EffectDerivation.Group(first));
            }

            Debug.LogError($"[LookDerivation] No head for {(skill != null ? skill.GetType().Name : "a unit without skill")}");
            return HeadKind.Bud;
        }

        // The projectile class and its baked motion, the same reading for a unit's head and its shot
        public static HeadKind Delivery(GameObject projectilePrefab)
        {
            if (projectilePrefab == null)
            {
                return HeadKind.Bud;
            }

            ChainLightningProjectile chain = projectilePrefab.GetComponent<ChainLightningProjectile>();
            if (chain != null)
            {
                return SkillWalker.IsHeld(chain) ? HeadKind.Fork : HeadKind.Conductor;
            }

            if (projectilePrefab.GetComponent<CurvedHomingProjectileBehaviour>() != null
                || projectilePrefab.GetComponent<ArcHomingProjectileBehaviour>() != null)
            {
                return HeadKind.Arch;
            }

            HomingProjectileBehaviour homing = projectilePrefab.GetComponent<HomingProjectileBehaviour>();
            if (homing != null && homing.data != null && homing.data.speed >= SpearSpeed)
            {
                return HeadKind.Spear;
            }
            return HeadKind.Bud;
        }

        // One slot: a second skill or delivery, then a baked behaviour, then the first passive, then an on-hit effect
        public static AccessoryKind Accessory(EntityData data)
        {
            if (data == null)
            {
                return AccessoryKind.None;
            }

            ASkillFactory primary = Primary(data);
            if (TryMiniHead(data, out HeadKind miniHead))
            {
                return AccessoryKind.MiniHead;
            }

            foreach (GameObject prefab in SkillWalker.Prefabs(primary))
            {
                if (prefab.GetComponent<BackstabProjectileBehaviour>() != null)
                {
                    return AccessoryKind.Hook;
                }

                if (prefab.GetComponent<IncreaseDamageOnDistanceProjectileBehaviour>() != null)
                {
                    return AccessoryKind.Antenna;
                }
            }

            if (data.passives != null && data.passives.Count > 0 && data.passives[0] != null)
            {
                AccessoryKind passive = PassiveAccessory(data.passives[0]);
                if (passive != AccessoryKind.None)
                {
                    return passive;
                }
            }

            if (data.onHitEffects != null)
            {
                EffectFamily accent = Accent(primary);
                foreach (ABuffHandlerFactory handler in data.onHitEffects)
                {
                    EffectFamily family = EffectDerivation.Family(handler, false);
                    if (handler == null || family == accent)
                    {
                        continue;
                    }

                    if (family == EffectFamily.Rot)
                    {
                        return AccessoryKind.DripBeads;
                    }

                    if (family == EffectFamily.Bane)
                    {
                        return AccessoryKind.ShardBarbs;
                    }

                    if (family == EffectFamily.Boon)
                    {
                        return AccessoryKind.SmallTorus;
                    }
                }
            }
            return AccessoryKind.None;
        }

        public static HeadKind AccessoryHead(EntityData data)
        {
            TryMiniHead(data, out HeadKind head);
            return head;
        }

        // The family the primary delivers, drawn on the tips and the projectile
        public static EffectFamily Accent(ASkillFactory skill)
        {
            if (skill is ShootProjectileSkillFactory shoot && shoot.data.projectiles != null)
            {
                foreach (ShootProjectileSkillData.ProjectileData entry in shoot.data.projectiles)
                {
                    if (entry.onHitConsumer != null && entry.onHitConsumer.Count > 0)
                    {
                        return EffectDerivation.ConsumerFamily(entry.onHitConsumer[0], 1f, false);
                    }
                }
                return EffectFamily.Damage;
            }

            if (skill is ApplyBuffOnTargetSkillFactory support)
            {
                return EffectDerivation.Family(support.data.buffHandlerFactory, support.data.targetAlly);
            }

            if (skill is HealTargetSkillFactory)
            {
                return EffectFamily.Heal;
            }

            if (skill is ApplyConsumerOnTimeFactory self)
            {
                return EffectDerivation.ConsumerFamily(self.data.consumerFactory, 1f, true);
            }

            if (skill is ApplyBuffPeriodicallySkillFactory periodic && periodic.data.periodicBuff != null
                && periodic.data.periodicBuff.Count > 0)
            {
                return IsEveryBoon(periodic.data.periodicBuff)
                    ? EffectFamily.Boon
                    : EffectDerivation.Family(periodic.data.periodicBuff[0], true);
            }
            return EffectFamily.Damage;
        }

        // A second skill, else a second delivery inside the primary, drawn as a small head
        static bool TryMiniHead(EntityData data, out HeadKind head)
        {
            head = HeadKind.Bud;
            ASkillFactory secondary = Secondary(data);
            if (secondary != null)
            {
                head = Head(secondary);
                return true;
            }

            ASkillFactory primary = Primary(data);
            if (primary == null)
            {
                return false;
            }

            HeadKind main = Head(primary);
            foreach (GameObject prefab in SkillWalker.Prefabs(primary))
            {
                HeadKind other = Delivery(prefab);
                if (other != main)
                {
                    head = other;
                    return true;
                }
            }
            return false;
        }

        static AccessoryKind PassiveAccessory(ABuffHandlerFactory handler)
        {
            if (handler.buffFactoryList == null)
            {
                return AccessoryKind.None;
            }

            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (buff is CurrentWaveModifierFactory)
                {
                    return AccessoryKind.TierRings;
                }
            }

            EffectFamily family = EffectDerivation.Family(handler, true);
            if (family == EffectFamily.Renew)
            {
                return AccessoryKind.StalkBeads;
            }

            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (EffectDerivation.TryModifier(buff, out AttributeType type, out float delta))
                {
                    return family == EffectFamily.Bane ? AccessoryKind.ConeCrown : AccessoryKind.SmallTorus;
                }
            }
            return AccessoryKind.None;
        }

        static HeadKind Gift(EffectFamily family, AttributeGroup group)
        {
            switch (family)
            {
                case EffectFamily.Heal:
                case EffectFamily.Renew:
                    return HeadKind.GiftHeal;
                case EffectFamily.Boon:
                    return group == AttributeGroup.Offence ? HeadKind.GiftBoonOffence : HeadKind.GiftBoonDefence;
                default:
                    return HeadKind.GiftBane;
            }
        }

        static bool IsEveryBoon(List<ABuffHandlerFactory> handlers)
        {
            foreach (ABuffHandlerFactory handler in handlers)
            {
                if (EffectDerivation.Family(handler, true) != EffectFamily.Boon)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
