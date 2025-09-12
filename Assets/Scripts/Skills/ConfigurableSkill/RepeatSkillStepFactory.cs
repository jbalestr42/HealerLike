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
            ASkillStep skillStep = skillStepFactory.AddSkillStep();
            skillStep.Init();
            _skillSteps.Add(skillStep);
        }
    }

    public override bool Update(ASkill skill, float deltaRepeat)
    {
        if (_skillSteps[_currentStep].Update(skill, Time.deltaTime))
        {
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

    public override void Reset()
    {
        _currentCount = 0;
        _currentStep = 0;

        // Only reset the first step, all other steps will be reset when they are selected
        _skillSteps[_currentStep].Reset();
        // foreach (var skillStep in _skillSteps)
        // {
        //     skillStep.Reset();
        // }
    }
}
