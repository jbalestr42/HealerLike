using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowestHealthTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.LowestHealth;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Sort((GameObject a, GameObject b) =>
        {
            return a.GetComponent<Entity>().health.Value.CompareTo(b.GetComponent<Entity>().health.Value);
        });
    }
}