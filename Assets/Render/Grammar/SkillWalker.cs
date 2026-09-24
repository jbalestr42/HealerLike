using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Walks the skill, step, projectile and value trees of an EntityData, the derivations read their numbers here
    public static class SkillWalker
    {
        // One projectile prefab of a skill and how many times it fires in one cycle
        public class Shot
        {
            public GameObject prefab;
            public float count;
        }

        // ChainLightningProjectile keeps its mode private, it is read through its own serialization
        // TODO: read ChainLightningProjectile.effectMode once ChainLightningProjectile exposes it
        public static bool IsHeld(ChainLightningProjectile chain)
        {
            ChainModeReading reading = new ChainModeReading();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(chain), reading);
            return reading._effectMode == ChainLightningProjectile.EffectMode.AttackRateDuration;
        }

        [Serializable]
        class ChainModeReading
        {
            // Named as the serialized field of ChainLightningProjectile so the JSON matches it
            public ChainLightningProjectile.EffectMode _effectMode = ChainLightningProjectile.EffectMode.FixedDuration;
        }

        // The prefab shooting the most per cycle, the first one on a tie
        public static GameObject DominantPrefab(ASkillFactory skill)
        {
            GameObject dominant = null;
            float most = 0f;
            foreach (Shot shot in Shots(skill))
            {
                if (shot.count > most)
                {
                    most = shot.count;
                    dominant = shot.prefab;
                }
            }
            return dominant;
        }

        // Every projectile prefab of the skill, in the order the data lists them
        public static List<GameObject> Prefabs(ASkillFactory skill)
        {
            List<GameObject> prefabs = new List<GameObject>();
            foreach (Shot shot in Shots(skill))
            {
                prefabs.Add(shot.prefab);
            }
            return prefabs;
        }

        // Shots per prefab in one cycle of the skill, in the order the data lists them: each entry fires once per cycle,
        // a step once per execution
        public static List<Shot> Shots(ASkillFactory skill)
        {
            List<Shot> shots = new List<Shot>();
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

        // Seconds of waiting in one loop of a burst
        public static float Waits(List<ASkillStepFactory> steps, EntityData data)
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

        // An AValue read on the unit's own data, as the steps read it on the unit at play time.
        // Without data every base reads 1, so the value keeps the sign of what it scales
        public static float Value(AValue value, EntityData data)
        {
            if (value is FlatValue flat)
            {
                return flat.data.value;
            }

            if (value is AttributeValue attribute)
            {
                return Base(data, ReadAttribute(data, attribute.data.type, 0f)) * attribute.data.multiplier;
            }

            if (value is MaxHealthValue maxHealth)
            {
                return Base(data, LookDerivation.Health(data)) * maxHealth.data.multiplier;
            }

            if (value is CurrentHealthValue currentHealth)
            {
                // A unit spawns at full health, so the health it misses is none
                float health = 0f;
                if (!currentHealth.data.inverse)
                {
                    health = LookDerivation.Health(data);
                }
                return Base(data, health) * currentHealth.data.multiplier;
            }

            if (value != null)
            {
                Debug.LogError($"[SkillWalker] No reading for the value {value.GetType().Name}");
            }
            return 0f;
        }

        public static float ReadAttribute(EntityData data, AttributeType type, float fallback)
        {
            if (data == null || data.attributes == null || !data.attributes.ContainsKey(type))
            {
                return fallback;
            }
            return data.attributes[type];
        }

        public static int Bounces(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0;
            }

            BounceProjectileBehaviour bounce = prefab.GetComponent<BounceProjectileBehaviour>();
            if (bounce == null || bounce.data == null)
            {
                return 0;
            }
            return bounce.data.bounce;
        }

        // Bounces installed by a passive of the unit, items never count
        public static int PassiveBounces(EntityData data)
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

        public static bool HasSplash(ASkillFactory skill, EntityData data)
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

        static void AddShots(List<ASkillStepFactory> steps, float repeats, List<Shot> shots)
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

        static void AddShot(List<Shot> shots, GameObject prefab, float count)
        {
            if (prefab == null)
            {
                return;
            }

            foreach (Shot shot in shots)
            {
                if (shot.prefab == prefab)
                {
                    shot.count += count;
                    return;
                }
            }
            shots.Add(new Shot() { prefab = prefab, count = count });
        }

        // The base a value scales, or 1 when there is no unit to read it on
        static float Base(EntityData data, float unitBase)
        {
            if (data == null)
            {
                return 1f;
            }
            return unitBase;
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
