using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.Random;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Shuffle();
    }
}