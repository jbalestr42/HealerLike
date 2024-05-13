using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class AOnSkillTriggerFactory : SerializedScriptableObject
{
    public abstract AOnSkillTrigger GetSkillTrigger();
}

public class OnSkillTriggerFactory<OnSkillTriggerType, OnSkillTriggerData> : AOnSkillTriggerFactory where OnSkillTriggerType : AOnSkillTrigger<OnSkillTriggerData>, new()
{
    [InlineProperty]
    [HideLabel]
    public OnSkillTriggerData data;

    public override AOnSkillTrigger GetSkillTrigger()
    {
        OnSkillTriggerType skill = new OnSkillTriggerType();
        skill.data = data;
        return skill;
    }
}

public abstract class AOnSkillTrigger
{
    public abstract void Execute(GameObject source);
}

public abstract class AOnSkillTrigger<OnSkillTriggerData> : AOnSkillTrigger
{
    [InlineProperty]
    [HideLabel]
    public OnSkillTriggerData data;
}