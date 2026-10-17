using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/EqualizeHealthSkillStep")]
public class EqualizeHealthSkillStepFactory : SkillStepFactory<EqualizeHealthSkillStep, EqualizeHealthSkillStepData> { }

[Serializable]
public class EqualizeHealthSkillStepData : SkillStepDataBase
{
    // The enemies in range that are equalized (e.g. not the untargetable ones)
    public List<ATargetValidatorFactory> targetValidators = new List<ATargetValidatorFactory>();
    // How the shared share of max health is computed (by default, the average of the shares)
    public BalanceLifeMode mode = BalanceLifeMode.Relative;
}

// Sets every enemy in range to the same share of its max health, by default the average of theirs, taking no time (e.g. the
// Arbiter: a full tank and a dying unit both end at half health). The health is set directly: no armor, shield nor
// heal modifier changes it
public class EqualizeHealthSkillStep : ASkillStep<EqualizeHealthSkillStepData>
{
    EveryEnemyInRange _everyTarget;

    public override void Init()
    {
    }

    // Waits for the requirements of the skill, like a shot (e.g. an enemy in range)
    public override bool Update(ASkill skill, ref float deltaTime)
    {
        if (!skill.IsRequirementValidated())
        {
            return false;
        }
        HealthBalance.Balance(FindEveryTarget(skill.gameObject), data.mode);
        return true;
    }

    public override void Reset()
    {
    }

    // Every enemy within the range of the unit
    protected virtual List<GameObject> FindEveryTarget(GameObject unit)
    {
        _everyTarget ??= new EveryEnemyInRange(data.targetValidators);
        return _everyTarget.Find(unit);
    }
}
