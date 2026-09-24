using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Reads the look of a unit from its EntityData, never from its title, name or asset path
    public static partial class LookDerivation
    {
        // Homing prefabs this fast read as a spear: StraightLaserBullet flies at 20, BulletSpeed at 15
        public static readonly float SpearSpeed = 18f;
        public static readonly int FewHits = 2;
        public static readonly int ManyHits = 4;
        public static readonly float QuickCadence = 0.5f;
        public static readonly float SlowCadence = 1.5f;
        public static readonly float LightHealth = 120f;
        public static readonly float HeavyHealth = 250f;
        // Range in cells, a guess: nothing in the game data says what a short range is
        public static readonly float ShortRange = 3f;
        public static readonly float MidRange = 8f;
        public static readonly float DefaultHealth = 100f;
        public static readonly float DefaultAttackRate = 1f;
        public static readonly float DefaultRange = 1000f;

        public static UnitChannels Channels(EntityData data, Entity.EntityType entityType)
        {
            ASkillFactory primary = Primary(data);
            UnitChannels channels = new UnitChannels();
            channels.side = Side(entityType);
            channels.head = Head(primary);
            channels.count = Count(Hits(primary, data));
            channels.stem = Stem(Cadence(primary, data));
            channels.mass = Mass(Health(data));
            channels.reach = Reach(data);
            channels.accent = Accent(primary);
            channels.accessory = Accessory(data, channels.head, channels.accent, out channels.accessoryHead);
            return channels;
        }

        public static LookSide Side(Entity.EntityType entityType)
        {
            return entityType == Entity.EntityType.Player ? LookSide.Plant : LookSide.Stone;
        }

        // TODO: read EntityData.primarySkill once EntityData declares one, the first skill is the primary until then
        public static ASkillFactory Primary(EntityData data)
        {
            if (data == null || data.skillFactories == null || data.skillFactories.Count == 0)
            {
                return null;
            }
            return data.skillFactories[0];
        }

        public static ASkillFactory Secondary(EntityData data)
        {
            if (data == null || data.skillFactories == null || data.skillFactories.Count < 2)
            {
                return null;
            }
            return data.skillFactories[1];
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
                if (SkillWalker.IsHeld(chain))
                {
                    return HeadKind.Fork;
                }
                return HeadKind.Conductor;
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

        // Hits per trigger of the skill: shots per target plus bounces, one loop of a burst, many for any splash
        public static int Hits(ASkillFactory skill, EntityData data)
        {
            if (skill is AreaOfEffectSkillFactory || SkillWalker.HasSplash(skill, data))
            {
                return ManyHits;
            }

            int bounces = SkillWalker.PassiveBounces(data);
            if (skill is ShootProjectileSkillFactory shoot && shoot.data.projectiles != null)
            {
                int hits = 1;
                foreach (ShootProjectileSkillData.ProjectileData entry in shoot.data.projectiles)
                {
                    hits = Mathf.Max(hits, entry.numberOfProjectileToShootPerTarget + SkillWalker.Bounces(entry.projectilePrefab) + bounces);
                }
                return hits;
            }

            if (skill is ConfigurableSkillFactory configurable)
            {
                float total = 0f;
                foreach (SkillWalker.Shot shot in SkillWalker.Shots(configurable))
                {
                    total += shot.count;
                }
                return Mathf.Max(1, Mathf.RoundToInt(total) + bounces);
            }
            return 1;
        }

        public static CountBand Count(int hits)
        {
            if (hits >= ManyHits)
            {
                return CountBand.Many;
            }
            if (hits >= FewHits)
            {
                return CountBand.Few;
            }
            return CountBand.One;
        }

        // The skill's own clock in seconds between triggers, never the raw AttackRate
        public static float Cadence(ASkillFactory skill, EntityData data)
        {
            if (skill is ShootProjectileSkillFactory)
            {
                return SkillWalker.ReadAttribute(data, AttributeType.AttackRate, DefaultAttackRate);
            }

            if (skill is ConfigurableSkillFactory configurable)
            {
                return SkillWalker.Waits(configurable.data.skillStepFactories, data);
            }

            if (skill is ApplyBuffOnTargetSkillFactory support)
            {
                return support.data.rate;
            }

            if (skill is HealTargetSkillFactory heal)
            {
                return heal.data.rate;
            }

            if (skill is AreaOfEffectSkillFactory)
            {
                float rate = SkillWalker.ReadAttribute(data, AttributeType.AttackRate, DefaultAttackRate);
                if (rate > 0f)
                {
                    return 1f / rate;
                }
                return 0f;
            }

            if (skill is ApplyConsumerOnTimeFactory self)
            {
                return self.data.rate;
            }

            if (skill is ApplyBuffPeriodicallySkillFactory periodic && periodic.data.periodicBuff != null)
            {
                float total = 0f;
                foreach (ABuffHandlerFactory handler in periodic.data.periodicBuff)
                {
                    if (handler != null)
                    {
                        total += handler.duration;
                    }
                }
                return total;
            }
            return DefaultAttackRate;
        }

        public static StemBand Stem(float cadence)
        {
            if (cadence <= QuickCadence)
            {
                return StemBand.Quick;
            }
            if (cadence <= SlowCadence)
            {
                return StemBand.Steady;
            }
            return StemBand.Slow;
        }

        // HealthMax after the flat modifiers the unit's own items put on it, as it spawns
        public static float Health(EntityData data)
        {
            float health = SkillWalker.ReadAttribute(data, AttributeType.HealthMax, DefaultHealth);
            float added = 0f;
            float multiplier = 1f;
            foreach (ABuffHandlerFactory handler in SkillWalker.ItemBuffs(data))
            {
                if (handler.buffFactoryList == null)
                {
                    continue;
                }

                foreach (ABuffFactory buff in handler.buffFactoryList)
                {
                    FlatModifierFactory flat = buff as FlatModifierFactory;
                    if (flat == null || flat.data.type != AttributeType.HealthMax)
                    {
                        continue;
                    }

                    if (flat.data.modifierType == AttributeModifierType.Add)
                    {
                        added += flat.data.value;
                    }
                    else if (flat.data.modifierType == AttributeModifierType.Multiply)
                    {
                        multiplier *= 1f + flat.data.value;
                    }
                }
            }
            return (health + added) * multiplier;
        }

        public static MassBand Mass(float health)
        {
            if (health <= LightHealth)
            {
                return MassBand.Light;
            }
            if (health <= HeavyHealth)
            {
                return MassBand.Sturdy;
            }
            return MassBand.Heavy;
        }

        public static ReachBand Reach(EntityData data)
        {
            float range = SkillWalker.ReadAttribute(data, AttributeType.Range, DefaultRange);
            if (range <= ShortRange)
            {
                return ReachBand.Short;
            }
            if (range <= MidRange)
            {
                return ReachBand.Mid;
            }
            return ReachBand.Long;
        }
    }
}
