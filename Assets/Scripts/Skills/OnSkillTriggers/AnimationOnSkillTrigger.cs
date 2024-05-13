using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/OnSkillTriggers/AnimationOnSkillTrigger")]
public class AnimationOnSkillTriggerFactory : OnSkillTriggerFactory<AnimationOnSkillTrigger, AnimationOnSkillTriggerData> {}

[Serializable]
public class AnimationOnSkillTriggerData
{
    public string animationName;
}

public class AnimationOnSkillTrigger : AOnSkillTrigger<AnimationOnSkillTriggerData>
{
    public override void Execute(GameObject source)
    {
        // TODO: start animation
        source.GetComponentInChildren<DeviantBoss>().StartAnimation();
    }
}