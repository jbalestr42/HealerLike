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

        public static bool IsHeld(ChainLightningProjectile chain)
        {
            return ChainProjectileReading.IsHeld(chain);
        }

        // The prefab shooting the most per cycle, the first one on a tie
        public static GameObject DominantPrefab(ASkillFactory skill)
        {
            return SkillProjectiles.Dominant(Shots(skill));
        }

        // The projectile prefabs the skill fires, combined in authored order
        public static List<GameObject> Prefabs(ASkillFactory skill)
        {
            List<GameObject> prefabs = new List<GameObject>();
            foreach (Shot shot in Shots(skill))
            {
                prefabs.Add(shot.prefab);
            }
            return prefabs;
        }

        // Shots per cycle; configurable steps reset to their first entry before each execution.
        public static List<Shot> Shots(ASkillFactory skill)
        {
            return SkillDescriptionReader.Read(skill, null).shots;
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
                if (step is RepeatSkillStepFactory repeat && repeat.data != null)
                {
                    total += repeat.data.count * Waits(repeat.data.skillStepFactories, data);
                }
                else if (step is DurationSkillStepFactory duration && duration.data != null)
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
                if (flat.data == null)
                {
                    return 0f;
                }

                return flat.data.value;
            }

            if (value is AttributeValue attribute)
            {
                if (attribute.data == null)
                {
                    return 0f;
                }

                return Base(data, ReadAttribute(data, attribute.data.type, 0f)) * attribute.data.multiplier;
            }

            if (value is MaxHealthValue maxHealth)
            {
                if (maxHealth.data == null)
                {
                    return 0f;
                }

                return Base(data, LookDerivation.Health(data)) * maxHealth.data.multiplier;
            }

            if (value is CurrentHealthValue currentHealth)
            {
                if (currentHealth.data == null)
                {
                    return 0f;
                }

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
            return ProjectileDescriptionReader.Read(prefab).bounces;
        }

        public static bool HasSplash(ASkillFactory skill, EntityData data)
        {
            return SkillDescriptionReader.HasSplash(Shots(skill), data);
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
    }
}
