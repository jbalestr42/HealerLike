using System;
using UnityEngine;
using Oisif.Inspector;

[InlineEditor]
public abstract class ASkillStepFactory : ScriptableObject
{
    public abstract ASkillStep AddSkillStep(GameObject source);

    // The step shoots at the targets of the entity, itself or one of its inner steps
    public virtual bool shootsAtTargets => false;
}

public class SkillStepFactory<SkillStepType, SkillStepData> : ASkillStepFactory
                                where SkillStepType : ASkillStep<SkillStepData>, new()
                                where SkillStepData : SkillStepDataBase
{
    [InlineProperty]
    public SkillStepData data;

    public override ASkillStep AddSkillStep(GameObject source)
    {
        SkillStepType skillStep = new SkillStepType() { data = this.data, source = source };;
        return skillStep;
    }
}

public abstract class ASkillStep
{
    public GameObject source;

    public abstract void Init();
    // True once the step is done. Spends what it needs of deltaTime, the rest goes to the next step
    public abstract bool Update(ASkill skill, ref float deltaTime);
    public abstract void Reset();

    // Seconds the step lasts, 0 for an instant one (e.g. a shot)
    public virtual float GetDuration()
    {
        return 0f;
    }

    // Seconds spent in the step since it started, up to its duration
    public virtual float GetElapsed()
    {
        return 0f;
    }
}

[Serializable]
public class SkillStepDataBase
{
}

public abstract class ASkillStep<SkillStepData> : ASkillStep where SkillStepData : SkillStepDataBase
{
    public SkillStepData data;
}
