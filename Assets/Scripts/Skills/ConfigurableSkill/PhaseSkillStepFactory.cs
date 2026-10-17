using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/PhaseSkillStep")]
public class PhaseSkillStepFactory : SkillStepFactory<PhaseSkillStep, PhaseSkillStepData>
{
    public override bool shootsAtTargets => data != null && data.skillStepFactories.Exists(factory => factory != null && factory.shootsAtTargets);
}

[Serializable]
public class PhaseSkillStepData : SkillStepDataBase
{
    // Shown above the unit while the phase plays (e.g. "Slam", filling up until the slam lands)
    public string title;
    public Sprite icon;
    // Played once each, in order
    [CreateDataButton]
    public List<ASkillStepFactory> skillStepFactories = new List<ASkillStepFactory>();
}

// A named part of the cycle of a ConfigurableSkill, its steps played in order (e.g. a wind-up then a slam), shown
// above the unit for the players to see it coming
public class PhaseSkillStep : ASkillStep<PhaseSkillStepData>
{
    RepeatSkillStep _sequence;

    public override void Init()
    {
        _sequence = new RepeatSkillStep
        {
            data = new RepeatSkillStepData { skillStepFactories = data.skillStepFactories, count = 1 },
            source = source,
        };
        _sequence.Init();
    }

    public override bool Update(ASkill skill, ref float deltaTime)
    {
        if (data.skillStepFactories.Count == 0)
        {
            return true;
        }
        return _sequence.Update(skill, ref deltaTime);
    }

    public override void Reset()
    {
        if (data.skillStepFactories.Count > 0)
        {
            _sequence.Reset();
        }
    }

    public override float GetDuration()
    {
        return data.skillStepFactories.Count > 0 ? _sequence.GetDuration() : 0f;
    }

    public override float GetElapsed()
    {
        return data.skillStepFactories.Count > 0 ? _sequence.GetElapsed() : 0f;
    }

    // Part of the phase played, 0 when it starts and 1 at its end (an instant phase is always at its start)
    public float GetProgress()
    {
        float duration = GetDuration();
        return duration > 0f ? Mathf.Clamp01(GetElapsed() / duration) : 0f;
    }
}
