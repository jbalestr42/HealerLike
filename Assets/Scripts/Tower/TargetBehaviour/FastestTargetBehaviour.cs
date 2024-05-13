using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastestTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.Fastest;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Sort((GameObject a, GameObject b) =>
        {
            return a.GetComponent<CheckPointMove>().speed.CompareTo(b.GetComponent<CheckPointMove>().speed);
        });
    }
}