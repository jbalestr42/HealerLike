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

            string name = "a unit without skill";
            if (skill != null)
            {
                name = skill.GetType().Name;
            }
            Debug.LogError($"[LookDerivation] No head for {name}");
            return HeadKind.Bud;
        }

        // One slot: a second skill or delivery, then a baked behaviour, then the first passive, then an on-hit effect
        public static AccessoryKind Accessory(EntityData data)
        {
            ASkillFactory primary = Primary(data);
            return Accessory(data, PrimaryHead(primary), Accent(primary), out HeadKind accessoryHead);
        }

        public static HeadKind AccessoryHead(EntityData data)
        {
            TryMiniHead(data, PrimaryHead(Primary(data)), out HeadKind head);
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
                        return EffectDerivation.ConsumerFamily(entry.onHitConsumer[0], false);
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
                return EffectDerivation.ConsumerFamily(self.data.consumerFactory, true);
            }

            if (skill is ApplyBuffPeriodicallySkillFactory periodic && periodic.data.periodicBuff != null
                && periodic.data.periodicBuff.Count > 0)
            {
                if (IsEveryBoon(periodic.data.periodicBuff))
                {
                    return EffectFamily.Boon;
                }
                return EffectDerivation.Family(periodic.data.periodicBuff[0], true);
            }
            return EffectFamily.Damage;
        }

        // The accessory and its small head, read once from the head and the accent already derived
        static AccessoryKind Accessory(EntityData data, HeadKind head, EffectFamily accent, out HeadKind accessoryHead)
        {
            if (TryMiniHead(data, head, out accessoryHead))
            {
                return AccessoryKind.MiniHead;
            }

            if (data == null)
            {
                return AccessoryKind.None;
            }

            foreach (GameObject prefab in SkillWalker.Prefabs(Primary(data)))
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
                foreach (ABuffHandlerFactory handler in data.onHitEffects)
                {
                    if (handler == null)
                    {
                        continue;
                    }

                    EffectFamily family = EffectDerivation.Family(handler, false);
                    if (family == accent)
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

        // The head of the primary skill, Bud for a unit without one
        static HeadKind PrimaryHead(ASkillFactory primary)
        {
            if (primary == null)
            {
                return HeadKind.Bud;
            }
            return Head(primary);
        }

        // A second skill, else a second delivery inside the primary, drawn as a small head
        static bool TryMiniHead(EntityData data, HeadKind main, out HeadKind head)
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
                    if (family == EffectFamily.Bane)
                    {
                        return AccessoryKind.ConeCrown;
                    }
                    return AccessoryKind.SmallTorus;
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
                    if (group == AttributeGroup.Offence)
                    {
                        return HeadKind.GiftBoonOffence;
                    }
                    return HeadKind.GiftBoonDefence;
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
