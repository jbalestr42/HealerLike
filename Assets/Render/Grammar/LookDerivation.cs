using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public enum LookSide
    {
        Plant,
        Stone
    }

    // How the primary skill delivers, the unit's main silhouette
    public enum HeadKind
    {
        Bud,
        Spear,
        Arch,
        Conductor,
        Fork,
        GiftHeal,
        GiftBoonDefence,
        GiftBoonOffence,
        GiftBane,
        Ward,
        Pulse,
        SelfTick
    }

    public enum CountBand
    {
        One,
        Few,
        Many
    }

    public enum StemBand
    {
        Quick,
        Steady,
        Slow
    }

    public enum MassBand
    {
        Light,
        Sturdy,
        Heavy
    }

    public enum ReachBand
    {
        Short,
        Mid,
        Long
    }

    public enum AccessoryKind
    {
        None,
        MiniHead,
        Hook,
        Antenna,
        ThornCollar,
        TierRings,
        TwinSeeds,
        StalkBeads,
        SmallTorus,
        ConeCrown,
        DripBeads,
        ShardBarbs
    }

    // Everything the look of a unit reads from its data, decided once at spawn
    public struct UnitChannels
    {
        public LookSide side;
        public HeadKind head;
        public CountBand count;
        public StemBand stem;
        public MassBand mass;
        public ReachBand reach;
        public AccessoryKind accessory;
        // The head drawn small when the accessory is a mini head
        public HeadKind accessoryHead;
        public EffectFamily accent;
    }

    // Reads the look of a unit from his EntityData, never from its title, name or asset path
    public static class LookDerivation
    {
        // Homing prefabs this fast read as a spear: his laser flies at 20, his bullet at 15
        public static readonly float SpearSpeed = 18f;
        public static readonly int FewHits = 2;
        public static readonly int ManyHits = 4;
        public static readonly float QuickCadence = 0.5f;
        public static readonly float SlowCadence = 1.5f;
        public static readonly float LightHealth = 120f;
        public static readonly float HeavyHealth = 250f;
        // Range in cells, a guess until he says what a short range is
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
            channels.accessory = Accessory(data);
            channels.accessoryHead = AccessoryHead(data);
            channels.accent = Accent(primary);
            return channels;
        }

        public static LookSide Side(Entity.EntityType entityType)
        {
            return entityType == Entity.EntityType.Player ? LookSide.Plant : LookSide.Stone;
        }

        // TODO: the first skill is the primary until he declares one on EntityData
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

        public static HeadKind Head(ASkillFactory skill)
        {
            if (skill is ShootProjectileSkillFactory || skill is ConfigurableSkillFactory)
            {
                return Delivery(DominantPrefab(skill));
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
                return IsHeld(chain) ? HeadKind.Fork : HeadKind.Conductor;
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
            if (skill is AreaOfEffectSkillFactory || HasSplash(skill, data))
            {
                return ManyHits;
            }

            int bounces = PassiveBounces(data);
            if (skill is ShootProjectileSkillFactory shoot && shoot.data.projectiles != null)
            {
                int hits = 1;
                foreach (ShootProjectileSkillData.ProjectileData entry in shoot.data.projectiles)
                {
                    hits = Mathf.Max(hits, entry.numberOfProjectileToShootPerTarget + Bounces(entry.projectilePrefab) + bounces);
                }
                return hits;
            }

            if (skill is ConfigurableSkillFactory configurable)
            {
                Dictionary<GameObject, float> shots = new Dictionary<GameObject, float>();
                AddShots(configurable.data.skillStepFactories, 1f, shots);
                float total = 0f;
                foreach (KeyValuePair<GameObject, float> pair in shots)
                {
                    total += pair.Value;
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
            return hits >= FewHits ? CountBand.Few : CountBand.One;
        }

        // The skill's own clock in seconds between triggers, never the raw AttackRate
        public static float Cadence(ASkillFactory skill, EntityData data)
        {
            if (skill is ShootProjectileSkillFactory)
            {
                return ReadAttribute(data, AttributeType.AttackRate, DefaultAttackRate);
            }

            if (skill is ConfigurableSkillFactory configurable)
            {
                return Waits(configurable.data.skillStepFactories, data);
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
                float rate = ReadAttribute(data, AttributeType.AttackRate, DefaultAttackRate);
                return rate > 0f ? 1f / rate : 0f;
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
                    total += handler != null ? handler.duration : 0f;
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
            return cadence <= SlowCadence ? StemBand.Steady : StemBand.Slow;
        }

        // HealthMax after the unit's own flat modifier passives, as it spawns
        public static float Health(EntityData data)
        {
            float health = ReadAttribute(data, AttributeType.HealthMax, DefaultHealth);
            if (data == null || data.passives == null)
            {
                return health;
            }

            float added = 0f;
            float multiplier = 1f;
            foreach (ABuffHandlerFactory handler in data.passives)
            {
                if (handler == null || handler.buffFactoryList == null)
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
            return health <= HeavyHealth ? MassBand.Sturdy : MassBand.Heavy;
        }

        public static ReachBand Reach(EntityData data)
        {
            float range = ReadAttribute(data, AttributeType.Range, DefaultRange);
            if (range <= ShortRange)
            {
                return ReachBand.Short;
            }
            return range <= MidRange ? ReachBand.Mid : ReachBand.Long;
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

            foreach (GameObject prefab in Prefabs(primary))
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
            foreach (GameObject prefab in Prefabs(primary))
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

        // His ChainLightningProjectile keeps its mode private, it is read through his own serialization
        // TODO: read a public effectMode once ChainLightningProjectile exposes one
        static bool IsHeld(ChainLightningProjectile chain)
        {
            ChainModeReading reading = new ChainModeReading();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(chain), reading);
            return reading._effectMode == ChainLightningProjectile.EffectMode.AttackRateDuration;
        }

        [Serializable]
        class ChainModeReading
        {
            // Named as his serialized field so the JSON matches it
            public ChainLightningProjectile.EffectMode _effectMode = ChainLightningProjectile.EffectMode.FixedDuration;
        }

        // The prefab shooting the most per cycle, the first one on a tie
        static GameObject DominantPrefab(ASkillFactory skill)
        {
            Dictionary<GameObject, float> shots = Shots(skill);
            GameObject dominant = null;
            float most = 0f;
            foreach (GameObject prefab in Prefabs(skill))
            {
                if (shots[prefab] > most)
                {
                    most = shots[prefab];
                    dominant = prefab;
                }
            }
            return dominant;
        }

        // Every projectile prefab of the skill, in the order the data lists them
        static List<GameObject> Prefabs(ASkillFactory skill)
        {
            List<GameObject> prefabs = new List<GameObject>();
            if (skill is ShootProjectileSkillFactory shoot && shoot.data.projectiles != null)
            {
                foreach (ShootProjectileSkillData.ProjectileData entry in shoot.data.projectiles)
                {
                    if (entry.projectilePrefab != null && !prefabs.Contains(entry.projectilePrefab))
                    {
                        prefabs.Add(entry.projectilePrefab);
                    }
                }
            }
            else if (skill is ConfigurableSkillFactory configurable)
            {
                AddPrefabs(configurable.data.skillStepFactories, prefabs);
            }
            return prefabs;
        }

        static void AddPrefabs(List<ASkillStepFactory> steps, List<GameObject> prefabs)
        {
            if (steps == null)
            {
                return;
            }

            foreach (ASkillStepFactory step in steps)
            {
                if (step is RepeatSkillStepFactory repeat)
                {
                    AddPrefabs(repeat.data.skillStepFactories, prefabs);
                }
                else if (step is ShootProjectileSkillStepFactory shoot && shoot.data.projectiles != null)
                {
                    foreach (ShootProjectileSkillStepData.ProjectileData entry in shoot.data.projectiles)
                    {
                        if (entry.projectilePrefab != null && !prefabs.Contains(entry.projectilePrefab))
                        {
                            prefabs.Add(entry.projectilePrefab);
                        }
                    }
                }
            }
        }

        // Shots per prefab in one cycle of the skill: each entry fires once per cycle, a step once per execution
        static Dictionary<GameObject, float> Shots(ASkillFactory skill)
        {
            Dictionary<GameObject, float> shots = new Dictionary<GameObject, float>();
            if (skill is ShootProjectileSkillFactory shoot && shoot.data.projectiles != null)
            {
                foreach (ShootProjectileSkillData.ProjectileData entry in shoot.data.projectiles)
                {
                    AddShot(shots, entry.projectilePrefab, entry.numberOfProjectileToShootPerTarget);
                }
            }
            else if (skill is ConfigurableSkillFactory configurable)
            {
                AddShots(configurable.data.skillStepFactories, 1f, shots);
            }
            return shots;
        }

        static void AddShots(List<ASkillStepFactory> steps, float repeats, Dictionary<GameObject, float> shots)
        {
            if (steps == null)
            {
                return;
            }

            foreach (ASkillStepFactory step in steps)
            {
                if (step is RepeatSkillStepFactory repeat)
                {
                    AddShots(repeat.data.skillStepFactories, repeats * repeat.data.count, shots);
                }
                else if (step is ShootProjectileSkillStepFactory shoot && shoot.data.projectiles != null
                         && shoot.data.projectiles.Count > 0)
                {
                    // The step cycles its entries, one per execution
                    float executions = repeats / shoot.data.projectiles.Count;
                    foreach (ShootProjectileSkillStepData.ProjectileData entry in shoot.data.projectiles)
                    {
                        AddShot(shots, entry.projectilePrefab, executions * entry.numberOfProjectileToShootPerTarget);
                    }
                }
            }
        }

        static void AddShot(Dictionary<GameObject, float> shots, GameObject prefab, float count)
        {
            if (prefab == null)
            {
                return;
            }

            float previous = shots.ContainsKey(prefab) ? shots[prefab] : 0f;
            shots[prefab] = previous + count;
        }

        // Seconds of waiting in one loop of a burst
        static float Waits(List<ASkillStepFactory> steps, EntityData data)
        {
            float total = 0f;
            if (steps == null)
            {
                return total;
            }

            foreach (ASkillStepFactory step in steps)
            {
                if (step is RepeatSkillStepFactory repeat)
                {
                    total += repeat.data.count * Waits(repeat.data.skillStepFactories, data);
                }
                else if (step is DurationSkillStepFactory duration)
                {
                    total += Value(duration.data.duration, data);
                }
            }
            return total;
        }

        // An AValue read on the unit's own data, as his steps read it on the unit at play time
        static float Value(AValue value, EntityData data)
        {
            if (value is FlatValue flat)
            {
                return flat.data.value;
            }

            if (value is AttributeValue attribute)
            {
                return ReadAttribute(data, attribute.data.type, 0f) * attribute.data.multiplier;
            }

            if (value is MaxHealthValue maxHealth)
            {
                return Health(data) * maxHealth.data.multiplier;
            }

            if (value is CurrentHealthValue currentHealth)
            {
                return currentHealth.data.inverse ? 0f : Health(data) * currentHealth.data.multiplier;
            }
            return 0f;
        }

        static float ReadAttribute(EntityData data, AttributeType type, float fallback)
        {
            if (data == null || data.attributes == null || !data.attributes.ContainsKey(type))
            {
                return fallback;
            }
            return data.attributes[type];
        }

        static int Bounces(GameObject prefab)
        {
            BounceProjectileBehaviour bounce = prefab != null ? prefab.GetComponent<BounceProjectileBehaviour>() : null;
            return bounce != null && bounce.data != null ? bounce.data.bounce : 0;
        }

        // Bounces installed by a passive of the unit, items never count
        static int PassiveBounces(EntityData data)
        {
            int bounces = 0;
            foreach (AProjectileBehaviourFactory behaviour in PassiveBehaviours(data))
            {
                if (behaviour is BounceProjectileBehaviourFactory bounce)
                {
                    bounces += bounce.data.bounce;
                }
            }
            return bounces;
        }

        static bool HasSplash(ASkillFactory skill, EntityData data)
        {
            foreach (GameObject prefab in Prefabs(skill))
            {
                if (prefab.GetComponent<AreaOfEffectProjectileBehaviour>() != null)
                {
                    return true;
                }
            }

            foreach (AProjectileBehaviourFactory behaviour in PassiveBehaviours(data))
            {
                if (behaviour is AreaOfEffectProjectileBehaviourFactory)
                {
                    return true;
                }
            }
            return false;
        }

        static List<AProjectileBehaviourFactory> PassiveBehaviours(EntityData data)
        {
            List<AProjectileBehaviourFactory> behaviours = new List<AProjectileBehaviourFactory>();
            if (data == null || data.passives == null)
            {
                return behaviours;
            }

            foreach (ABuffHandlerFactory handler in data.passives)
            {
                if (handler == null || handler.buffFactoryList == null)
                {
                    continue;
                }

                foreach (ABuffFactory buff in handler.buffFactoryList)
                {
                    if (buff is ProjectileBehaviourBuffFactory projectileBuff && projectileBuff.data.projectileBehaviour != null)
                    {
                        behaviours.Add(projectileBuff.data.projectileBehaviour);
                    }
                }
            }
            return behaviours;
        }
    }
}
