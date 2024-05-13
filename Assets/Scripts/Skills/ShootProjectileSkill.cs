using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ShootProjectileSkillData : SkillDataBase
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public GameObject projectilePrefab;

    public int numberOfProjectileToShootPerTarget = 1;
}

public class ShootProjectileSkill : ASkill<ShootProjectileSkillData>
{
    Attribute _cooldownDuration;

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
            foreach (GameObject target in targets)
            {
                for (int i = 0; i < data.numberOfProjectileToShootPerTarget; i++)
                {
                    SkillSource skillSource = entity.skillStartPoint;
                    skillSource.OnUseSkill();

                    GameObject projectileGo = Instantiate(data.projectilePrefab, skillSource.transform.position, Quaternion.identity);
                    Projectile projectile = projectileGo.GetComponent<Projectile>();
                    projectile.Init(source, target, entity.projectileBehaviours);
                }
            }
            return true;
        }
        return false;
    }

    public override float cooldownDuration => 1f / _cooldownDuration.Value;
}