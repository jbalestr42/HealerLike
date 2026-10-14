using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/ApplyBuffSkillStep")]
public class ApplyBuffSkillStepFactory : SkillStepFactory<ApplyBuffSkillStep, ApplyBuffSkillStepData> { }

[Serializable]
public class ApplyBuffSkillStepData : SkillStepDataBase
{
    // Given to the unit using the skill (e.g. the guard of the Armored Footman after its strikes)
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Gives a buff to the unit using the skill, taking no time (e.g. strikes twice, then raises its guard)
public class ApplyBuffSkillStep : ASkillStep<ApplyBuffSkillStepData>
{
    public override void Init()
    {
    }

    public override bool Update(ASkill skill, ref float deltaTime)
    {
        BuffManager buffManager = source != null ? source.GetComponent<BuffManager>() : null;
        if (buffManager != null && data.buffHandlerFactory != null)
        {
            buffManager.AddHandler(data.buffHandlerFactory, source, source);
        }
        return true;
    }

    public override void Reset()
    {
    }
}
