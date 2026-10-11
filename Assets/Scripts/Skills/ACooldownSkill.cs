using UnityEngine;

public abstract class ACooldownSkill<SkillData> : ASkill<SkillData>, ICooldownSkill where SkillData : SkillDataBase
{
    float _cooldown = 0f;

    // Battle time before the next use, 0 when it is ready
    public float timeBeforeUse => Mathf.Max(0f, _cooldown);

    public override void UpdateBehaviour(GameObject source)
    {
        Tick(source, Time.deltaTime);
    }

    // The cooldown runs during the whole update, even the one where it ends, and what goes beyond it counts
    // for the next one: a skill is used as often whatever the frame rate
    public void Tick(GameObject source, float deltaTime)
    {
        if (_cooldown > 0f)
        {
            _cooldown -= deltaTime;
        }

        // TODO how to get source and target here ? regardless of isSelfTarget
        // Return true if skill has been used
        if (_cooldown <= 0f && Execute(source))
        {
            foreach (AOnSkillTriggerFactory factory in data.onSkillTriggerFactory)
            {
                AOnSkillTrigger onSkillTrigger = factory.GetSkillTrigger();
                onSkillTrigger.Execute(source); // source and target
            }
            _cooldown += cooldownDuration;
        }
    }

    public override void Reset()
    {
        _cooldown = 0f;
    }

    // Sets the time left before the next use, 0 to use it as soon as possible
    public void InitCooldown(float cooldown)
    {
        _cooldown = Mathf.Max(0f, cooldown);
    }

    public abstract bool Execute(GameObject source);

    #region ICooldownSkill

    public float cooldownProgress => _cooldown / cooldownDuration;

    public abstract float cooldownDuration { get; }

    #endregion
}
