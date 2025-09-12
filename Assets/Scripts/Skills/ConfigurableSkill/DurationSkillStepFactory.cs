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

    public override bool Update(ASkill skill, float deltaTime)
    {
        _timer += deltaTime;
        if (_timer >= data.duration.GetValue(source))
        {
            // Debug.LogWarning($"Duration done {data.duration.GetValue(source)}");
            return true;
        }
        return false;
    }

    public override void Reset()
    {
        // Debug.LogWarning($"Reset Duration {data.duration.GetValue(source)}");
        _timer = 0f;
    }
}
