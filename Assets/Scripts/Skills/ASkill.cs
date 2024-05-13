using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[InlineEditor]
public abstract class ASkillFactory : SerializedScriptableObject
{
    public abstract ASkill AddSkill(GameObject target);
}

public class SkillFactory<SkillType, SkillData> : ASkillFactory
                                where SkillType : ASkill<SkillData>, new()
                                where SkillData : SkillDataBase
{
    [InlineProperty]
    [HideLabel]
    public SkillData data;

    public override ASkill AddSkill(GameObject target)
    {
        SkillType skill = target.AddComponent<SkillType>();
        skill.data = data;
        return skill;
    }
}

public abstract class ASkill : MonoBehaviour
{
    List<IRequirement> _requirements;
    public List<IRequirement> requirements { get { return _requirements; } set { _requirements = value; } }

    void Update()
    {
        UpdateBehaviour(gameObject);
    }

    public bool IsRequirementValidated()
    {
        if (_requirements != null)
        {
            foreach (IRequirement requirement in _requirements)
            {
                if (!requirement.IsValid(gameObject))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public abstract void UpdateBehaviour(GameObject source);
}

public class SkillDataBase
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AOnSkillTriggerFactory>, AOnSkillTriggerFactory>(onSkillTriggerFactory)")]
    public List<AOnSkillTriggerFactory> onSkillTriggerFactory;
}

public abstract class ASkill<SkillData> : ASkill where SkillData : SkillDataBase
{
    [InlineProperty]
    [HideLabel]
    public SkillData data;

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

    public abstract bool Execute(GameObject source);
}