using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.First;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        // Order is by default the nearest to the end
    }
}