using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/SkillSteps/RepeatSkillStep")]
public class RepeatSkillStepFactory : SkillStepFactory<RepeatSkillStep, RepeatSkillStepData> { }

[Serializable]
public class RepeatSkillStepData : SkillStepDataBase
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillStepFactory>, ASkillStepFactory>(skillStepFactories)")]
    public List<ASkillStepFactory> skillStepFactories = new List<ASkillStepFactory>();
    public int count;
}

public class RepeatSkillStep : ASkillStep<RepeatSkillStepData>
{
    int _currentCount = 0;
    int _currentStep = 0;
    List<ASkillStep> _skillSteps = new List<ASkillStep>();

    public override void Init()
    {
        _currentStep = 0;
        foreach (var skillStepFactory in data.skillStepFactories)
        {
            ASkillStep skillStep = skillStepFactory.AddSkillStep(source);
            skillStep.Init();
            _skillSteps.Add(skillStep);
        }
    }

    // The time left by a step goes on in the next ones during the same update, at most every repeat in one update
    public override bool Update(ASkill skill, ref float deltaRepeat)
    {
        for (int played = 0; played < _skillSteps.Count * Mathf.Max(1, data.count); played++)
        {
            if (!_skillSteps[_currentStep].Update(skill, ref deltaRepeat))
            {
                return false;
            }
            _currentStep++;

            if (_currentStep >= _skillSteps.Count)
            {
                _currentStep = 0;
                _currentCount++;

                // Check if the repeat is done
                if (_currentCount >= data.count)
                {
                    return true;
                }
            }

            // Reset the step before updating it
            _skillSteps[_currentStep].Reset();
        }
        return false;
    }

    // The steps repeated once
    float GetCycleDuration()
    {
        float duration = 0f;
        foreach (ASkillStep skillStep in _skillSteps)
        {
            duration += skillStep.GetDuration();
        }
        return duration;
    }

    public override float GetDuration()
    {
        return data.count * GetCycleDuration();
    }

    public override float GetElapsed()
    {
        if (_skillSteps.Count == 0)
        {
            return 0f;
        }

        float elapsed = _currentCount * GetCycleDuration();
        for (int i = 0; i < _currentStep; i++)
        {
            elapsed += _skillSteps[i].GetDuration();
        }
        return Mathf.Min(elapsed + _skillSteps[_currentStep].GetElapsed(), GetDuration());
    }

    public override void Reset()
    {
        _currentCount = 0;
        _currentStep = 0;

        // Only reset the first step, all other steps will be reset when they are selected
        _skillSteps[_currentStep].Reset();
    }
}
