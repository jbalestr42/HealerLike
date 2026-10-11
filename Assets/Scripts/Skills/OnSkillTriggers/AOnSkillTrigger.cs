using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[InlineEditor]
public abstract class AOnSkillTriggerFactory : ScriptableObject
{
    public abstract AOnSkillTrigger GetSkillTrigger();
}

public class OnSkillTriggerFactory<OnSkillTriggerType, OnSkillTriggerData> : AOnSkillTriggerFactory, IGameDataSource where OnSkillTriggerType : AOnSkillTrigger<OnSkillTriggerData>, new()
{
    [InlineProperty]
    public OnSkillTriggerData data;
    public object sourceData { get { return data; } }

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

public abstract class AOnSkillTrigger<OnSkillTriggerData> : AOnSkillTrigger, IGameDataSource
{
    public OnSkillTriggerData data;
    public object sourceData { get { return data; } }
}