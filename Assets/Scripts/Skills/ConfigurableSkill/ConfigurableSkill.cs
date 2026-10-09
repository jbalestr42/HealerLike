using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public class ConfigurableSkillData : SkillDataBase
{
    [CreateDataButton]
    public List<ASkillStepFactory> skillStepFactories = new List<ASkillStepFactory>();
}

public class ConfigurableSkill : ASkill<ConfigurableSkillData>, ICooldownSkill
{
    int _currentStep = 0;
    List<ASkillStep> _skillSteps = new List<ASkillStep>();

    void Start()
    {
        foreach (var skillStepFactory in data.skillStepFactories)
        {
            ASkillStep skillStep = skillStepFactory.AddSkillStep(gameObject);
            skillStep.Init();
            _skillSteps.Add(skillStep);
        }
    }

    public override void UpdateBehaviour(GameObject source)
    {
        Tick(source, Time.deltaTime);
    }

    // The time left by a step goes on in the next ones during the same update, at most one pass over the steps:
    // a cycle lasts as long whatever the frame rate
    public void Tick(GameObject source, float deltaTime)
    {
        for (int played = 0; played < _skillSteps.Count; played++)
        {
            if (!_skillSteps[_currentStep].Update(this, ref deltaTime))
            {
                return;
            }

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

    #region ICooldownSkill

    // ICooldownSkill: the cooldown is a whole cycle of the steps (e.g. a burst of shots then a pause)
    public float cooldownDuration
    {
        get
        {
            float duration = 0f;
            foreach (ASkillStep skillStep in _skillSteps)
            {
                duration += skillStep.GetDuration();
            }
            return duration;
        }
    }

    // Part of the cycle left, 1 when it starts and 0 at its end
    public float cooldownProgress
    {
        get
        {
            float duration = cooldownDuration;
            if (duration <= 0f)
            {
                return 0f;
            }

            float elapsed = 0f;
            for (int i = 0; i < _currentStep; i++)
            {
                elapsed += _skillSteps[i].GetDuration();
            }
            elapsed += _skillSteps[_currentStep].GetElapsed();
            return Mathf.Clamp01((duration - elapsed) / duration);
        }
    }

    #endregion
}