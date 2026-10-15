using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/ShootProjectileSkillStep")]
public class ShootProjectileSkillStepFactory : SkillStepFactory<ShootProjectileSkillStep, ShootProjectileSkillStepData>
{
    public override bool shootsAtTargets => true;
}

[Serializable]
public class ShootProjectileSkillStepData : SkillStepDataBase
{
    public List<ProjectileData> projectiles;
    // Shot at every enemy in range instead of the targets of the entity (e.g. a strike on the whole team)
    public bool atEveryTarget;
    // Only with atEveryTarget: the enemies in range that can be hit (e.g. not the untargetable ones)
    public List<ATargetValidatorFactory> targetValidators = new List<ATargetValidatorFactory>();
}

public class ShootProjectileSkillStep : ASkillStep<ShootProjectileSkillStepData>
{
    int _projectileIndex = 0;
    // Its own way to pick targets, without limit nor taunt, the targets of the entity left untouched
    ATargetBehaviour _everyTargetBehaviour;

    public override void Init()
    {
    }

    // A shot takes no time
    public override bool Update(ASkill skill, ref float deltaTime)
    {
        if (skill.IsRequirementValidated())
        {
            List<GameObject> targets = data.atEveryTarget ? FindEveryTarget(skill.gameObject) : null;
            ProjectileAttack.Shoot(skill.gameObject, data.projectiles[_projectileIndex], targets);
            _projectileIndex = (_projectileIndex + 1) % data.projectiles.Count;
            return true;
        }
        return false;
    }

    public override void Reset()
    {
        _projectileIndex = 0;
    }

    // Every enemy within the range of the unit, a copy
    protected virtual List<GameObject> FindEveryTarget(GameObject unit)
    {
        if (_everyTargetBehaviour == null)
        {
            _everyTargetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.First);
            _everyTargetBehaviour.targetCount = 0;
            foreach (ATargetValidatorFactory targetValidator in data.targetValidators)
            {
                _everyTargetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
            }
        }
        float range = unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Range).Value;
        return new List<GameObject>(_everyTargetBehaviour.GetTargets(unit, unit.transform.position, range, unit.GetComponent<Entity>().GetTargetType()));
    }
}
