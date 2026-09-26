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
                if (step is RepeatSkillStepFactory repeat)
                {
                    AddShots(repeat.data.skillStepFactories, repeats * repeat.data.count, shots);
                }
                else if (step is ShootProjectileSkillStepFactory shoot && shoot.data.projectiles != null
                         && shoot.data.projectiles.Count > 0)
                {
                    // Existing cosmetic averaging. Gameplay resets multi-entry steps differently;
                    // keep authored channels stable until that interpretation changes explicitly.
                    float executions = repeats / shoot.data.projectiles.Count;
                    foreach (ShootProjectileSkillStepData.ProjectileData entry in shoot.data.projectiles)
                    {
                        AddShot(shots, entry.projectilePrefab, executions * entry.numberOfProjectileToShootPerTarget);
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
