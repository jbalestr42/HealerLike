using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ConfigurableSkillData : SkillDataBase
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillStepFactory>, ASkillStepFactory>(skillStepFactories)")]
    public List<ASkillStepFactory> skillStepFactories = new List<ASkillStepFactory>();
}

public class ConfigurableSkill : ASkill<ConfigurableSkillData>
{
    int _currentStep = 0;
    List<ASkillStep> _skillSteps = new List<ASkillStep>();

    void Start()
    {
        foreach (var skillStepFactory in data.skillStepFactories)
        {
            ASkillStep skillStep = skillStepFactory.AddSkillStep();
            skillStep.Init();
            _skillSteps.Add(skillStep);
        }
    }

    public override void UpdateBehaviour(GameObject source)
    {
        if (_skillSteps[_currentStep].Update(this, Time.deltaTime))
        {
            foreach (AOnSkillTriggerFactory factory in data.onSkillTriggerFactory)
            {
                AOnSkillTrigger onSkillTrigger = factory.GetSkillTrigger();
                onSkillTrigger.Execute(source);
            }

            _currentStep++;

            if (_currentStep >= _skillSteps.Count)
            {
                _currentStep = 0;
            }

            // Reset the step before updating it
            _skillSteps[_currentStep].Reset();
        }
    }

    public override void Reset()
    {
        _currentStep = 0;
        foreach (var skillStep in _skillSteps)
        {
            skillStep.Reset();
        }
    }
}