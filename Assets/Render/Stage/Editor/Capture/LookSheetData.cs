using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Builds game data in memory for the sheets. Every object made goes in the caller's list for it to destroy,
    // nothing is ever saved into any project folder
    public static class LookSheetData
    {
        public static readonly string EntityFolder = "Assets/Data/Entities/";
        public static readonly string ProjectileFolder = "Assets/Prefabs/Projectiles/";
        public static readonly string DamagePath = "Assets/Data/Entities/NormalEntity/AttributeConsumerFactory.asset";

        public static EntityData LoadEntity(string name)
        {
            // The HitArmorBuffer data folder carries the suffix twice
            string folder = name == "HitArmorBufferEntity" ? "HitArmorBufferEntityEntity" : name;
            return Load<EntityData>(EntityFolder + folder + "/" + name + ".asset");
        }

        public static ObjectType Load<ObjectType>(string path)
                                    where ObjectType : Object
        {
            ObjectType loaded = AssetDatabase.LoadAssetAtPath<ObjectType>(path);
            if (loaded == null)
            {
                Debug.LogError($"[LookSheetData] Nothing of type {typeof(ObjectType).Name} at {path}");
            }
            return loaded;
        }

        // An entity at the EntityData defaults, with the model every sheet unit borrows from Normal
        public static EntityData Entity(string title, List<Object> created)
        {
            EntityData normal = LoadEntity("NormalEntity");
            EntityData data = Track(ScriptableObject.CreateInstance<EntityData>(), created);
            data.name = title;
            data.title = title;
            data.model = normal.model;
            data.targetBehaviourType = TargetBehaviourType.First;
            data.targetValidators = new List<ATargetValidatorFactory>();
            data.passives = new List<ABuffHandlerFactory>();
            data.onHitEffects = new List<ABuffHandlerFactory>();
            data.skillFactories = new List<ASkillFactory>();
            data.attributes[AttributeType.HealthMax] = 100f;
            data.attributes[AttributeType.AttackRate] = 1f;
            data.attributes[AttributeType.Damage] = 4f;
            data.attributes[AttributeType.Range] = 100f;
            return data;
        }

        // A copy of one of the game's entities, sharing its skill, passive and on-hit assets
        public static EntityData Copy(EntityData source, string title, List<Object> created)
        {
            EntityData data = Entity(title, created);
            data.targetBehaviourType = source.targetBehaviourType;
            data.attributes = new Dictionary<AttributeType, float>(source.attributes);
            data.skillFactories.AddRange(source.skillFactories);
            if (source.passives != null)
            {
                data.passives.AddRange(source.passives);
            }

            if (source.onHitEffects != null)
            {
                data.onHitEffects.AddRange(source.onHitEffects);
            }
            return data;
        }

        public static BuffHandlerFactory Handler(DurationType durationType, float duration, float period, List<Object> created,
                                                 params ABuffFactory[] buffs)
        {
            BuffHandlerFactory handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>(), created);
            handler.data = new BuffHandlerData
            {
                durationType = durationType,
                duration = duration,
                isPeriodic = period > 0f,
                periodDuration = period,
                buffFactoryList = new List<ABuffFactory>(buffs),
                tags = new List<GameplayTag>()
            };
            return handler;
        }

        public static ConsumerFactory Flat(float value, List<Object> created)
        {
            ConsumerFactory consumer = Track(ScriptableObject.CreateInstance<ConsumerFactory>(), created);
            FlatValue flat = new FlatValue();
            flat.data = new FlatValueData { value = value };
            consumer.data = new ConsumerData { value = flat, ignoreDamageReduction = true };
            return consumer;
        }

        // Applies a flat consumer, negative heals
        public static ApplyConsumerBuffFactory Consume(float value, List<Object> created)
        {
            ApplyConsumerBuffFactory buff = Track(ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>(), created);
            buff.data = new ApplyConsumerBuffData { consumerFactory = Flat(value, created) };
            return buff;
        }

        public static FlatModifierFactory Modifier(AttributeType type, AttributeModifierType modifierType, float value,
                                                   List<Object> created)
        {
            FlatModifierFactory modifier = Track(ScriptableObject.CreateInstance<FlatModifierFactory>(), created);
            modifier.data = new FlatModifierData { type = type, modifierType = modifierType, value = value };
            return modifier;
        }

        public static CurrentWaveModifierFactory WaveModifier(AttributeType type, List<Object> created)
        {
            CurrentWaveModifierFactory modifier = Track(ScriptableObject.CreateInstance<CurrentWaveModifierFactory>(), created);
            modifier.data = new CurrentWaveModifierData { type = type, modifierType = AttributeModifierType.Multiply, value = 1f };
            return modifier;
        }

        public static ShootProjectileSkillFactory Shoot(GameObject prefab, int perTarget, List<Object> created)
        {
            ShootProjectileSkillFactory shoot = Track(ScriptableObject.CreateInstance<ShootProjectileSkillFactory>(), created);
            ShootProjectileSkillData.ProjectileData entry = new ShootProjectileSkillData.ProjectileData
            {
                projectilePrefab = prefab,
                onHitConsumer = new List<AConsumerFactory> { Load<AConsumerFactory>(DamagePath) },
                numberOfProjectileToShootPerTarget = perTarget
            };
            shoot.data = new ShootProjectileSkillData
            {
                onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                projectiles = new List<ShootProjectileSkillData.ProjectileData> { entry }
            };
            return shoot;
        }

        public static ApplyBuffOnTargetSkillFactory Support(ABuffHandlerFactory handler, bool targetAlly, float rate,
                                                            List<Object> created)
        {
            ApplyBuffOnTargetSkillFactory support = Track(ScriptableObject.CreateInstance<ApplyBuffOnTargetSkillFactory>(), created);
            support.data = new ApplyBuffOnTargetSkillData
            {
                onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                buffHandlerFactory = handler,
                targetValidators = new List<ATargetValidatorFactory>(),
                rate = rate,
                targetAlly = targetAlly
            };
            return support;
        }

        public static GameObject Prefab(string name)
        {
            return Load<GameObject>(ProjectileFolder + name + ".prefab");
        }

        // A copy of one of the game's projectile prefabs with behaviours baked in, kept under an inactive holder so it never
        // plays itself; EntityManager instantiates it under its own projectile parent, where it is active
        public static GameObject Variant(string prefab, List<Object> created, params AProjectileBehaviourFactory[] behaviours)
        {
            GameObject holder = Track(new GameObject("LookSheetVariant"), created);
            holder.SetActive(false);
            GameObject variantGo = Object.Instantiate(Prefab(prefab), holder.transform);
            variantGo.name = prefab + "Variant";
            foreach (AProjectileBehaviourFactory behaviour in behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.AddBehaviour(variantGo);
                }
            }
            return variantGo;
        }

        public static ObjectType Track<ObjectType>(ObjectType created, List<Object> list)
                                    where ObjectType : Object
        {
            list.Add(created);
            return created;
        }
    }
}
