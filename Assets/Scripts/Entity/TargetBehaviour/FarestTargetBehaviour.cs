using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarestTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.Farest;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Sort((GameObject a, GameObject b) =>
        {
            return Vector3.SqrMagnitude(b.transform.position - position).CompareTo(Vector3.SqrMagnitude(a.transform.position - position));
        });
    }
}