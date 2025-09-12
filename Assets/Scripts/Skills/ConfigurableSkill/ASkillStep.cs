using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ASkillStepFactory : SerializedScriptableObject
{
    public abstract ASkillStep AddSkillStep();
}

public class SkillStepFactory<SkillStepType, SkillStepData> : ASkillStepFactory
                                where SkillStepType : ASkillStep<SkillStepData>, new()
                                where SkillStepData : SkillStepDataBase
{
    [InlineProperty]
    [HideLabel]
    public SkillStepData data;

    public override ASkillStep AddSkillStep()
    {
        SkillStepType skillStep = new SkillStepType();
        skillStep.data = data;
        return skillStep;
    }
}

public abstract class ASkillStep
{
    public abstract void Init();
    public abstract bool Update(ASkill skill, float deltaTime);
    public abstract void Reset();
}

public class SkillStepDataBase
{
}

public abstract class ASkillStep<SkillStepData> : ASkillStep where SkillStepData : SkillStepDataBase
{
    [InlineProperty]
    [HideLabel]
    public SkillStepData data;
}
