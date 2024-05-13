using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NearestTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.Nearest;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Sort((GameObject a, GameObject b) =>
        {
            return Vector3.SqrMagnitude(a.transform.position - position).CompareTo(Vector3.SqrMagnitude(b.transform.position - position));
        });
    }
}