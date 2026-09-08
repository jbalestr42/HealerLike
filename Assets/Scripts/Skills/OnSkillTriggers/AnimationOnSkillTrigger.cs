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
        // Decoupled from any specific model script (e.g. DeviantBoss) so Scripts doesn't need
        // a compile-time reference into Assets/Models.
        source.BroadcastMessage("StartAnimation", SendMessageOptions.DontRequireReceiver);
    }
}