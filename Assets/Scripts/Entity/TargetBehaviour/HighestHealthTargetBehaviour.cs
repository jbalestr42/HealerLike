using System.Collections.Generic;
using UnityEngine;

// The unit with the most current health first (absolute value, not percent): the tanks and the units at
// full health, the opposite of LowestHealth
public class HighestHealthTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.HighestHealth;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Sort((GameObject a, GameObject b) =>
        {
            return b.GetComponent<Entity>().health.Value.CompareTo(a.GetComponent<Entity>().health.Value);
        });
    }
}
