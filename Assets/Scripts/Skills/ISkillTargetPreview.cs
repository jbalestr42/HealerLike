using UnityEngine;

// A skill used on one unit, which can tell ahead of time which one and when (see SkillTargetLines)
public interface ISkillTargetPreview
{
    // Battle time before the next use, 0 when it is ready
    float timeBeforeUse { get; }

    // The unit the skill would be used on now, null when it would not be used
    GameObject GetUpcomingTarget();
}
