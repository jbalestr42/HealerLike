using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Reads the look of a unit from its EntityData, never from its title, name or asset path
    public static class LookDerivation
    {
        // Homing prefabs this fast read as a spear: StraightLaserBullet flies at 20, BulletSpeed at 15
        public static readonly float SpearSpeed = 18f;
        public static readonly int FewHits = 2;
        public static readonly int ManyHits = 4;
        public static readonly float QuickCadence = 0.5f;
        public static readonly float SlowCadence = 1.5f;
        public static readonly float LightHealth = 120f;
        // The roster's 150-health Soldier stays Sturdy; the 200-health RandomShoot becomes Heavy.
        public static readonly float HeavyHealth = 175f;
        // The atlas dump found eight ranges at 100 and four at 1000; these are cosmetic bands.
        public static readonly float ShortRange = 100f;
        public static readonly float MidRange = 500f;
        public static readonly float DefaultHealth = 100f;
        public static readonly float DefaultAttackRate = 1f;
        public static readonly float DefaultRange = 1000f;

        public static UnitChannels Channels(EntityData data, Entity.EntityType entityType)
        {
            ASkillFactory primary = Primary(data);
            SkillDescription description = SkillDescriptionReader.Read(primary, data);
            UnitChannels channels = new UnitChannels();
            channels.side = Side(entityType);
            channels.head = description.head;
            channels.count = Count(description.hits);
            channels.stem = Stem(description.cadence);
            channels.mass = Mass(Health(data));
            channels.reach = Reach(data);
            channels.accent = description.accent;
            channels.accessory = HeadDerivation.Accessory(data, channels.head, channels.accent,
                out channels.accessoryHead);
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
        public static HeadKind DeliveryHead(GameObject projectilePrefab)
        {
            return ProjectileDescriptionReader.Read(projectilePrefab).head;
        }

        // Hits per trigger of the skill: shots per target plus bounces, one loop of a burst, many for any splash
        public static int Hits(ASkillFactory skill, EntityData data)
        {
            return SkillDescriptionReader.Read(skill, data).hits;
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
            return SkillDescriptionReader.Read(skill, data).cadence;
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
            foreach (ABuffHandlerFactory handler in ItemWalker.Buffs(data))
            {
                foreach (ABuffFactory buff in EffectDerivation.Buffs(handler))
                {
                    FlatModifierFactory flat = buff as FlatModifierFactory;
                    if (flat == null || flat.data == null || flat.data.type != AttributeType.HealthMax)
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
