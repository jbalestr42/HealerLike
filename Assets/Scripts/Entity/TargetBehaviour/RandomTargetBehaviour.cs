using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTargetBehaviour : ATargetBehaviour
{
    public override TargetBehaviourType targetType => TargetBehaviourType.Random;
    // Stays random, a taunting target is only as likely as the others
    protected override bool isTauntable => false;

    public override void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range)
    {
        targets.Shuffle();
    }
}