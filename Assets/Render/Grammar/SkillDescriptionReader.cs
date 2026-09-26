using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // This is the skill-factory boundary. Channel derivations consume its description, not concrete factories.
    public static class SkillDescriptionReader
    {
        public static SkillDescription Read(ASkillFactory skill, EntityData data)
        {
            SkillDescription description = new SkillDescription();
            if (skill is ShootProjectileSkillFactory shoot)
            {
                ReadShooter(shoot.data, data, description);
            }
            else if (skill is ConfigurableSkillFactory configurable)
            {
                description.shots = SkillProjectiles.Read(configurable.data);
                description.head = ProjectileHead(description.shots);
                description.cadence = SkillWalker.Waits(configurable.data.skillStepFactories, data);
                float total = 0f;
                foreach (SkillWalker.Shot shot in description.shots)
                {
                    total += shot.count;
                }
                description.hits = Mathf.Max(1, Mathf.RoundToInt(total) + ItemWalker.Bounces(data));
            }
            else if (skill is ApplyBuffOnTargetSkillFactory support)
            {
                ABuffHandlerFactory handler = support.data.buffHandlerFactory;
                description.accent = EffectDerivation.Family(handler, support.data.targetAlly);
                description.head = Gift(description.accent, EffectDerivation.Group(handler));
                description.cadence = support.data.rate;
            }
            else if (skill is HealTargetSkillFactory heal)
            {
                description.head = HeadKind.GiftHeal;
                description.accent = EffectFamily.Heal;
                description.cadence = heal.data.rate;
            }
            else if (skill is AreaOfEffectSkillFactory)
            {
                description.head = HeadKind.Pulse;
                description.hits = LookDerivation.ManyHits;
                float rate = SkillWalker.ReadAttribute(data, AttributeType.AttackRate,
                    LookDerivation.DefaultAttackRate);
                description.cadence = rate > 0f ? 1f / rate : 0f;
            }
            else if (skill is ApplyConsumerOnTimeFactory self)
            {
                description.head = HeadKind.SelfTick;
                description.accent = EffectDerivation.ConsumerFamily(self.data.consumerFactory, true);
                description.cadence = self.data.rate;
            }
            else if (skill is ApplyBuffPeriodicallySkillFactory periodic)
            {
                ReadPeriodic(periodic.data.periodicBuff, description);
            }
            else if (skill != null)
            {
                Debug.LogError($"[SkillDescriptionReader] No reading for {skill.GetType().Name}");
            }

            if (HasSplash(description.shots, data))
            {
                description.hits = LookDerivation.ManyHits;
            }
            return description;
        }

        static void ReadShooter(ShootProjectileSkillData skill, EntityData data, SkillDescription description)
        {
            description.shots = SkillProjectiles.Read(skill);
            description.head = ProjectileHead(description.shots);
            description.cadence = SkillWalker.ReadAttribute(data, AttributeType.AttackRate,
                LookDerivation.DefaultAttackRate);
            if (skill.projectiles == null)
            {
                return;
            }

            int bounces = ItemWalker.Bounces(data);
            bool hasAccent = false;
            foreach (ShootProjectileSkillData.ProjectileData entry in skill.projectiles)
            {
                description.hits = Mathf.Max(description.hits,
                    entry.numberOfProjectileToShootPerTarget + SkillWalker.Bounces(entry.projectilePrefab) + bounces);
                if (!hasAccent && entry.onHitConsumer != null && entry.onHitConsumer.Count > 0)
                {
                    description.accent = EffectDerivation.ConsumerFamily(entry.onHitConsumer[0], false);
                    hasAccent = true;
                }
            }
        }

        static void ReadPeriodic(List<ABuffHandlerFactory> handlers, SkillDescription description)
        {
            if (handlers == null)
            {
                return;
            }

            description.cadence = 0f;
            bool isEveryBoon = handlers.Count > 0;
            foreach (ABuffHandlerFactory handler in handlers)
            {
                if (handler != null)
                {
                    description.cadence += handler.duration;
                }
                if (EffectDerivation.Family(handler, true) != EffectFamily.Boon)
                {
                    isEveryBoon = false;
                }
            }

            if (isEveryBoon)
            {
                description.head = HeadKind.Ward;
                description.accent = EffectFamily.Boon;
            }
            else if (handlers.Count > 0)
            {
                ABuffHandlerFactory first = handlers[0];
                description.accent = EffectDerivation.Family(first, true);
                description.head = Gift(description.accent, EffectDerivation.Group(first));
            }
        }

        static HeadKind ProjectileHead(List<SkillWalker.Shot> shots)
        {
            return LookDerivation.DeliveryHead(SkillProjectiles.Dominant(shots));
        }

        public static bool HasSplash(List<SkillWalker.Shot> shots, EntityData data)
        {
            foreach (SkillWalker.Shot shot in shots)
            {
                if (ProjectileDescriptionReader.Read(shot.prefab).hasSplash)
                {
                    return true;
                }
            }
            foreach (AProjectileBehaviourFactory behaviour in ItemWalker.Behaviours(data))
            {
                if (behaviour is AreaOfEffectProjectileBehaviourFactory)
                {
                    return true;
                }
            }
            return false;
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
    }
}
