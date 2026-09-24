using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // The units on the sheet: every entity the game data holds today, the proposed roster and five invented units
    // units nobody designed, the last two built in memory only
    public static class LookSheetUnits
    {
        public static readonly string[] Roster =
        {
            "NormalEntity", "FastShootEntity", "TripleShootEntity", "MultiShotEntity", "RandomShootEntity",
            "ChainLightningEntity", "ChannelingEntity", "SwarmEntity", "TestEntity", "SoldierEntity", "HitArmorBufferEntity"
        };

        // The proposed roster units that are not in the game data yet, in roster order
        public static readonly string[] Proposed =
        {
            "Mender", "Warden", "Mortar", "Flanker", "Bramble", "Shieldbearer", "Brute", "Plague stone", "Hexer",
            "Mending stone", "Warded idol", "Rising stone", "Splitter", "Runner", "Warlord"
        };

        // A factory or an engine change the game does not have yet; the sheet draws the nearest data and stars the label
        public static readonly string[] StandIns = { "Mortar", "Bramble", "Splitter", "Runner" };

        public static readonly string[] Undesigned = { "Stormreed", "Puffball", "Old fern", "Needle stone", "Storm idol" };

        static readonly string[] stones =
        {
            "SoldierEntity", "HitArmorBufferEntity", "Shieldbearer", "Brute", "Plague stone", "Hexer", "Mending stone",
            "Warded idol", "Rising stone", "Splitter", "Runner", "Warlord", "Needle stone", "Storm idol"
        };

        static readonly string poisonPath = "Assets/Data/EntityItems/PoisonItem/BuffHandlerFactory.asset";
        static readonly string explosionPath = "Assets/Data/EntityItems/ExplodeOnHitItem/AreaOfEffectProjectileBehaviourFactory.asset";
        static readonly string distancePath =
            "Assets/Data/EntityItems/IncreaseDamageWithProjectileDistanceItem/IncreaseDamageOnDistanceProjectileBehaviourFactory.asset";
        static readonly string armorPath = "Assets/Data/Entities/HitArmorBufferEntityEntity/BuffHandlerFactory.asset";

        // Allies are plants and enemies stones, as the game's waves and the roster place them
        public static Entity.EntityType Side(string unit)
        {
            return Array.IndexOf(stones, unit) >= 0 ? Entity.EntityType.Computer : Entity.EntityType.Player;
        }

        public static bool IsStandIn(string unit)
        {
            return Array.IndexOf(StandIns, unit) >= 0;
        }

        public static string Label(string unit)
        {
            string label = unit.Replace("Entity", "").ToUpperInvariant();
            return IsStandIn(unit) ? label + "*" : label;
        }

        // The game's asset for a roster entity, else an EntityData that lives in memory only
        public static EntityData Create(string unit, List<Object> created)
        {
            if (Array.IndexOf(Roster, unit) >= 0)
            {
                return LookSheetData.LoadEntity(unit);
            }

            EntityData soldier = LookSheetData.LoadEntity("SoldierEntity");
            EntityData buffer = LookSheetData.LoadEntity("HitArmorBufferEntity");
            EntityData data = LookSheetData.Entity(unit, created);
            switch (unit)
            {
                case "Mender":
                case "Mending stone":
                    data.skillFactories.Add(Mend(created));
                    break;
                case "Warden":
                case "Shieldbearer":
                    data = LookSheetData.Copy(buffer, unit, created);
                    break;
                case "Mortar":
                    // Stand-in: the area behaviour baked into the prefab reads as the splash factory would
                    data.targetBehaviourType = TargetBehaviourType.Farest;
                    data.attributes[AttributeType.AttackRate] = 2f;
                    GameObject mortar = LookSheetData.Variant("LaserBullet", created,
                        LookSheetData.Load<AProjectileBehaviourFactory>(distancePath),
                        LookSheetData.Load<AProjectileBehaviourFactory>(explosionPath));
                    data.skillFactories.Add(LookSheetData.Shoot(mortar, 1, created));
                    break;
                case "Flanker":
                    data.targetBehaviourType = TargetBehaviourType.Nearest;
                    BackstabProjectileBehaviourFactory backstab =
                        LookSheetData.Track(ScriptableObject.CreateInstance<BackstabProjectileBehaviourFactory>(), created);
                    backstab.data = new BackstabProjectileBehaviourData();
                    GameObject flanker = LookSheetData.Variant("BulletSpeed", created, backstab);
                    data.skillFactories.Add(LookSheetData.Shoot(flanker, 1, created));
                    break;
                case "Bramble":
                    // Stand-in: the HP based damage half of the thorns, a passive with no on-hurt trigger
                    data.skillFactories.Add(LookSheetData.Shoot(LookSheetData.Prefab("BulletSpeed"), 1, created));
                    HPBasedModifierFactory thorns = LookSheetData.Track(ScriptableObject.CreateInstance<HPBasedModifierFactory>(), created);
                    thorns.data = new HPBasedModifierData
                    {
                        type = AttributeType.Damage,
                        modifierType = AttributeModifierType.Multiply,
                        factor = 1f,
                        threshold = 0.5f
                    };
                    LookSheetData.AddPassive(data, LookSheetData.Handler(DurationType.Infinite, 0f, 0f, created, thorns), created);
                    break;
                case "Brute":
                    data = LookSheetData.Copy(soldier, unit, created);
                    data.attributes[AttributeType.HealthMax] = 400f;
                    data.attributes[AttributeType.AttackRate] = 2.5f;
                    data.attributes[AttributeType.Damage] = 12f;
                    data.attributes[AttributeType.PercentArmor] = 0.3f;
                    break;
                case "Plague stone":
                    data = LookSheetData.Copy(soldier, unit, created);
                    LookSheetData.AddOnHitEffect(data, LookSheetData.Load<ABuffHandlerFactory>(poisonPath), created);
                    break;
                case "Hexer":
                    FlatModifierFactory slow = LookSheetData.Modifier(AttributeType.AttackRate, AttributeModifierType.Multiply, 0.5f, created);
                    BuffHandlerFactory hex = LookSheetData.Handler(DurationType.Duration, 4f, 0f, created, slow);
                    data.skillFactories.Add(LookSheetData.Support(hex, false, 4f, created));
                    break;
                case "Warded idol":
                    ApplyBuffPeriodicallySkillFactory ward =
                        LookSheetData.Track(ScriptableObject.CreateInstance<ApplyBuffPeriodicallySkillFactory>(), created);
                    InvincibilityBuffFactory invincibility =
                        LookSheetData.Track(ScriptableObject.CreateInstance<InvincibilityBuffFactory>(), created);
                    invincibility.data = new InvincibilityBuffData();
                    ward.data = new ApplyBuffPeriodicallySkillData
                    {
                        onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                        periodicBuff = new List<ABuffHandlerFactory>
                        {
                            LookSheetData.Handler(DurationType.Duration, 2f, 0f, created, invincibility),
                            LookSheetData.Handler(DurationType.Duration, 2f, 0f, created)
                        }
                    };
                    data.skillFactories.Add(ward);
                    break;
                case "Rising stone":
                    data = LookSheetData.Copy(soldier, unit, created);
                    LookSheetData.AddPassive(data, Rising(created), created);
                    break;
                case "Splitter":
                    // Stand-in: a passive that does nothing, where the spawn on death would sit
                    data = LookSheetData.Copy(soldier, unit, created);
                    LookSheetData.AddPassive(data, LookSheetData.Handler(DurationType.Infinite, 0f, 0f, created), created);
                    break;
                case "Runner":
                    // Stand-in: a soldier at melee range, nothing in the game walks
                    data = LookSheetData.Copy(soldier, unit, created);
                    data.targetBehaviourType = TargetBehaviourType.Nearest;
                    data.attributes[AttributeType.HealthMax] = 100f;
                    data.attributes[AttributeType.Range] = 1f;
                    break;
                case "Warlord":
                    data.attributes[AttributeType.HealthMax] = 400f;
                    data.skillFactories.Add(Volley(created));
                    data.skillFactories.Add(LookSheetData.Support(LookSheetData.Load<ABuffHandlerFactory>(armorPath), true, 5f, created));
                    LookSheetData.AddPassive(data, Rising(created), created);
                    break;
                default:
                    Undesign(unit, data, created);
                    break;
            }
            return data;
        }

        // Five units nobody designed, to test the grammar on inputs it was not tuned for
        static void Undesign(string unit, EntityData data, List<Object> created)
        {
            switch (unit)
            {
                case "Stormreed":
                    data.attributes[AttributeType.AttackRate] = 0.5f;
                    data.skillFactories.Add(LookSheetData.Shoot(LookSheetData.Prefab("ChannelingLightning"), 3, created));
                    break;
                case "Puffball":
                    data.attributes[AttributeType.HealthMax] = 200f;
                    AreaOfEffectSkillFactory pulse = LookSheetData.Track(ScriptableObject.CreateInstance<AreaOfEffectSkillFactory>(), created);
                    AreaOfEffectProjectileBehaviourFactory explosion = LookSheetData.Load<AreaOfEffectProjectileBehaviourFactory>(explosionPath);
                    pulse.data = new AreaOfEffectSkillData
                    {
                        onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                        areaOfEffectPrefab = explosion.data.areaOfEffectPrefab.gameObject
                    };
                    data.skillFactories.Add(pulse);
                    break;
                case "Old fern":
                    data.attributes[AttributeType.HealthMax] = 300f;
                    ApplyConsumerOnTimeFactory tick = LookSheetData.Track(ScriptableObject.CreateInstance<ApplyConsumerOnTimeFactory>(), created);
                    tick.data = new ApplyConsumerOnTimeData
                    {
                        onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                        consumerFactory = LookSheetData.Flat(-3f, created),
                        rate = 2f
                    };
                    data.skillFactories.Add(tick);
                    break;
                case "Needle stone":
                    data.attributes[AttributeType.AttackRate] = 0.5f;
                    data.skillFactories.Add(LookSheetData.Shoot(LookSheetData.Prefab("StraightLaserBullet"), 2, created));
                    break;
                case "Storm idol":
                    data.attributes[AttributeType.HealthMax] = 400f;
                    data.skillFactories.Add(LookSheetData.Shoot(LookSheetData.Prefab("ChainLightning"), 1, created));
                    LookSheetData.AddOnHitEffect(data, LookSheetData.Load<ABuffHandlerFactory>(poisonPath), created);
                    break;
                default:
                    Debug.LogError($"[LookSheetUnits] No unit named {unit}");
                    break;
            }
        }

        // Support heal on the most hurt ally: an instant flat heal of 12 that ignores reduction
        static ApplyBuffOnTargetSkillFactory Mend(List<Object> created)
        {
            BuffHandlerFactory heal = LookSheetData.Handler(DurationType.Instant, 0f, 0f, created, LookSheetData.Consume(-12f, created));
            ApplyBuffOnTargetSkillFactory mend = LookSheetData.Support(heal, true, 3f, created);
            HealthValidatorFactory hurt = LookSheetData.Track(ScriptableObject.CreateInstance<HealthValidatorFactory>(), created);
            hurt.data = new HealthValidatorData { threshold = 0.9f };
            IgnoreSelfValidatorFactory others = LookSheetData.Track(ScriptableObject.CreateInstance<IgnoreSelfValidatorFactory>(), created);
            others.data = new IgnoreSelfValidatorData();
            mend.data.targetValidators.Add(hurt);
            mend.data.targetValidators.Add(others);
            return mend;
        }

        static BuffHandlerFactory Rising(List<Object> created)
        {
            return LookSheetData.Handler(DurationType.Infinite, 0f, 0f, created,
                LookSheetData.WaveModifier(AttributeType.HealthMax, created),
                LookSheetData.WaveModifier(AttributeType.Damage, created));
        }

        // Three times: wait a second, then three curved bullets per target
        static ConfigurableSkillFactory Volley(List<Object> created)
        {
            DurationSkillStepFactory wait = LookSheetData.Track(ScriptableObject.CreateInstance<DurationSkillStepFactory>(), created);
            FlatValue second = new FlatValue();
            second.data = new FlatValueData { value = 1f };
            wait.data = new DurationSkillStepData { duration = second };

            ShootProjectileSkillStepFactory shoot = LookSheetData.Track(ScriptableObject.CreateInstance<ShootProjectileSkillStepFactory>(), created);
            ShootProjectileSkillStepData.ProjectileData entry = new ShootProjectileSkillStepData.ProjectileData
            {
                projectilePrefab = LookSheetData.Prefab("CurveBullet2"),
                onHitConsumer = new List<AConsumerFactory> { LookSheetData.Load<AConsumerFactory>(LookSheetData.DamagePath) },
                numberOfProjectileToShootPerTarget = 3
            };
            shoot.data = new ShootProjectileSkillStepData { projectiles = new List<ShootProjectileSkillStepData.ProjectileData> { entry } };

            RepeatSkillStepFactory repeat = LookSheetData.Track(ScriptableObject.CreateInstance<RepeatSkillStepFactory>(), created);
            repeat.data = new RepeatSkillStepData { count = 3, skillStepFactories = new List<ASkillStepFactory> { wait, shoot } };

            ConfigurableSkillFactory volley = LookSheetData.Track(ScriptableObject.CreateInstance<ConfigurableSkillFactory>(), created);
            volley.data = new ConfigurableSkillData
            {
                onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                skillStepFactories = new List<ASkillStepFactory> { repeat }
            };
            return volley;
        }
    }
}
