using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/DurationSkillStep")]
public class DurationSkillStepFactory : SkillStepFactory<DurationSkillStep, DurationSkillStepData> { }

[Serializable]
public class DurationSkillStepData : SkillStepDataBase
{
    //Can we use attribute modifier factory ?
    // Est-ce qu'il faut abstraire une simple valeur ? et qu'on utilise dans AConsumer aussi ?
    public float duration;
}

public class DurationSkillStep : ASkillStep<DurationSkillStepData>
{
    public float _timer;

    public override void Init()
    {
        _timer = 0f;
        Debug.LogWarning($"Init Duration {data.duration}");
    }

    public override bool Update(ASkill skill, float deltaTime)
    {
        _timer += deltaTime;
        if (_timer >= data.duration)
        {
            Debug.LogWarning($"Duration done {data.duration}");
            return true;
        }
        return false;
    }

    public override void Reset()
    {
        Debug.LogWarning($"Reset Duration {data.duration}");
        _timer = 0f;
    }
}
