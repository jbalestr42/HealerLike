using UnityEngine;

public abstract class ACooldownSkill<SkillData> : ASkill<SkillData> where SkillData : SkillDataBase
{
    float _cooldown = 0f;
    public float cooldownProgress => _cooldown / cooldownDuration;

    public abstract float cooldownDuration { get; }

    public override void UpdateBehaviour(GameObject source)
    {
        // TODO how to get source and target here ? regardless of isSelfTarget
        if (_cooldown <= 0f)
        {
            // Return true if skill has been used
            if (Execute(source))
            {
                foreach (AOnSkillTriggerFactory factory in data.onSkillTriggerFactory)
                {
                    AOnSkillTrigger onSkillTrigger = factory.GetSkillTrigger();
                    onSkillTrigger.Execute(source); // source and target
                }
                _cooldown += cooldownDuration;
            }
        }
        else
        {
            _cooldown -= Time.deltaTime;
        }
    }

    public override void Reset()
    {
        _cooldown = 0f;
    }

    public abstract bool Execute(GameObject source);
}
