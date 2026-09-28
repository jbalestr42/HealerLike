using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/ShootProjectileSkillStep")]
public class ShootProjectileSkillStepFactory : SkillStepFactory<ShootProjectileSkillStep, ShootProjectileSkillStepData> { }

[Serializable]
public class ShootProjectileSkillStepData : SkillStepDataBase
{
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
            ProjectileAttack.Shoot(skill.gameObject, data.projectiles[_projectileIndex]);
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
