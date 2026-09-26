using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // The projectile entries of a firing skill, combined by prefab in authored order.
    public static class SkillProjectiles
    {
        public static GameObject Dominant(List<SkillWalker.Shot> shots)
        {
            GameObject dominant = null;
            float most = 0f;
            foreach (SkillWalker.Shot shot in shots)
            {
                if (shot.count > most)
                {
                    most = shot.count;
                    dominant = shot.prefab;
                }
            }
            return dominant;
        }

        public static List<SkillWalker.Shot> Read(ShootProjectileSkillData data)
        {
            List<SkillWalker.Shot> shots = new List<SkillWalker.Shot>();
            if (data != null && data.projectiles != null)
            {
                foreach (ShootProjectileSkillData.ProjectileData entry in data.projectiles)
                {
                    if (entry == null)
                    {
                        continue;
                    }
                    AddShot(shots, entry.projectilePrefab, entry.numberOfProjectileToShootPerTarget);
                }
            }
            return shots;
        }

        public static List<SkillWalker.Shot> Read(ConfigurableSkillData data)
        {
            List<SkillWalker.Shot> shots = new List<SkillWalker.Shot>();
            if (data != null)
            {
                AddShots(data.skillStepFactories, 1f, shots);
            }
            return shots;
        }

        static void AddShots(List<ASkillStepFactory> steps, float repeats, List<SkillWalker.Shot> shots)
        {
            if (steps == null)
            {
                return;
            }

            foreach (ASkillStepFactory step in steps)
            {
                if (step is RepeatSkillStepFactory repeat && repeat.data != null)
                {
                    AddShots(repeat.data.skillStepFactories, repeats * repeat.data.count, shots);
                }
                else if (step is ShootProjectileSkillStepFactory shoot && shoot.data != null
                         && shoot.data.projectiles != null
                         && shoot.data.projectiles.Count > 0)
                {
                    // ConfigurableSkill and RepeatSkillStep reset each selected step before execution.
                    // ShootProjectileSkillStep.Reset selects entry zero, including every repeat.
                    ShootProjectileSkillStepData.ProjectileData entry = shoot.data.projectiles[0];
                    if (entry != null)
                    {
                        AddShot(shots, entry.projectilePrefab, repeats * entry.numberOfProjectileToShootPerTarget);
                    }
                }
            }
        }

        static void AddShot(List<SkillWalker.Shot> shots, GameObject prefab, float count)
        {
            if (prefab == null)
            {
                return;
            }

            foreach (SkillWalker.Shot shot in shots)
            {
                if (shot.prefab == prefab)
                {
                    shot.count += count;
                    return;
                }
            }
            shots.Add(new SkillWalker.Shot() { prefab = prefab, count = count });
        }

    }
}
