using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShootProjectileSkillData : SkillDataBase
{
    public List<ProjectileData> projectiles;
}

public class ShootProjectileSkill : ACooldownSkill<ShootProjectileSkillData>
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
            ProjectileAttack.Shoot(source, data.projectiles[_projectileIndex]);
            _projectileIndex = (_projectileIndex + 1) % data.projectiles.Count;
            return true;
        }
        return false;
    }

    public override float cooldownDuration => _cooldownDuration.Value;
}
