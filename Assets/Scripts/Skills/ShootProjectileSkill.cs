using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class ShootProjectileSkillData : SkillDataBase
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

public class ShootProjectileSkill : ASkill<ShootProjectileSkillData>
{
    Attribute _cooldownDuration;
    int _projectileIndex = 0;

    void Start()
    {
        requirements = new List<IRequirement>();
        requirements.Add(new TargetRequirement(gameObject));
        _cooldownDuration = GetComponent<AttributeManager>().Get(AttributeType.AttackRate);
    }

    public override bool Execute(GameObject source)
    {
        if (IsRequirementValidated())
        {
            ITargetProvider targetProvider = source.GetComponent<ITargetProvider>();
            Entity entity = source.GetComponent<Entity>();
            List<GameObject> targets = targetProvider.GetTargets();

            ShootProjectileSkillData.ProjectileData projectileData = data.projectiles[_projectileIndex];
            foreach (GameObject target in targets)
            {
                for (int i = 0; i < projectileData.numberOfProjectileToShootPerTarget; i++)
                {
                    SkillSource skillSource = entity.skillStartPoint;
                    skillSource.OnUseSkill();

                    GameObject projectileGo = EntityManager.instance.SpawnProjectile(projectileData.projectilePrefab, skillSource.transform.position, Quaternion.identity);
                    Projectile projectile = projectileGo.GetComponent<Projectile>();
                    projectile.Init(source, target, entity.projectileBehaviours, projectileData.onHitConsumer);
                }
            }
            _projectileIndex = (_projectileIndex + 1) % data.projectiles.Count;
            return true;
        }
        return false;
    }

    public override float cooldownDuration => 1f / _cooldownDuration.Value;
}