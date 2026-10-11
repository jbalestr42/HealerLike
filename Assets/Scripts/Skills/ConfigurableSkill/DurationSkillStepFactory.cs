using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/DurationSkillStep")]
public class DurationSkillStepFactory : SkillStepFactory<DurationSkillStep, DurationSkillStepData> { }

[Serializable]
public class DurationSkillStepData : SkillStepDataBase
{
    [SerializeReference]
    public AValue duration;
}

public class DurationSkillStep : ASkillStep<DurationSkillStepData>
{
    public float _timer;

    public override void Init()
    {
        _timer = 0f;
        // Debug.LogWarning($"Init Duration {data.duration.GetValue(source)}");
    }

    public override bool Update(ASkill skill, ref float deltaTime)
    {
        float duration = data.duration.GetValue(source);
        float spent = Mathf.Clamp(duration - _timer, 0f, deltaTime);
        _timer += spent;
        deltaTime -= spent;
        // Debug.LogWarning($"Duration done {data.duration.GetValue(source)}");
        return _timer >= duration;
    }

    public override float GetDuration()
    {
        return data.duration.GetValue(source);
    }

    public override float GetElapsed()
    {
        return Mathf.Min(_timer, GetDuration());
    }

    public override void Reset()
    {
        // Debug.LogWarning($"Reset Duration {data.duration.GetValue(source)}");
        _timer = 0f;
    }
}
