using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/ShootProjectileSkillStep")]
public class ShootProjectileSkillStepFactory : SkillStepFactory<ShootProjectileSkillStep, ShootProjectileSkillStepData> { }

[Serializable]
public class ShootProjectileSkillStepData : SkillStepDataBase
{
    [Serializable]
    public class ProjectileData
    {
        [HorizontalGroup("Split", 75)]
        [PreviewField(75)]
        [HideLabel]
        [AssetsOnly]
        public GameObject projectilePrefab;

        [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AConsumerFactory>, AConsumerFactory>(onHitConsumer)")]
        public List<AConsumerFactory> onHitConsumer;

        public int numberOfProjectileToShootPerTarget = 1;
    }

    public List<ProjectileData> projectiles;
}

public class ShootProjectileSkillStep : ASkillStep<ShootProjectileSkillStepData>
{
    int _projectileIndex = 0;

    public override void Init()
    {
    }

    public override bool Update(ASkill skill, float deltaTime)
    {
        if (skill.IsRequirementValidated())
        {
            ITargetProvider targetProvider = skill.gameObject.GetComponent<ITargetProvider>();
            Entity entity = skill.gameObject.GetComponent<Entity>();
            List<GameObject> targets = targetProvider.GetTargets();

            ShootProjectileSkillStepData.ProjectileData projectileData = data.projectiles[_projectileIndex];
            foreach (GameObject target in targets)
            {
                for (int i = 0; i < projectileData.numberOfProjectileToShootPerTarget; i++)
                {
                    SkillSource skillSource = entity.skillStartPoint;
                    skillSource.OnUseSkill();

                    GameObject projectileGo = EntityManager.instance.SpawnProjectile(projectileData.projectilePrefab, skillSource.transform.position, Quaternion.identity);
                    Projectile projectile = projectileGo.GetComponent<Projectile>();
                    projectile.Init(skill.gameObject, target, entity.projectileBehaviours, projectileData.onHitConsumer);
                }
            }
            _projectileIndex = (_projectileIndex + 1) % data.projectiles.Count;
            return true;
        }
        return false;
    }

    public override void Reset()
    {
        _projectileIndex = 0;
    }
}
