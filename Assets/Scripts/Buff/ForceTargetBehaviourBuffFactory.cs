using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ForceTargetBehaviourBuff")]
public class ForceTargetBehaviourBuffFactory : BuffFactory<ForceTargetBehaviourBuff, ForceTargetBehaviourBuffData> { }

[Serializable]
public class ForceTargetBehaviourBuffData
{
    // How the holder picks its targets as long as the buff lasts (e.g. Poison spreading its stacks at random)
    public TargetBehaviourType targetBehaviourType = TargetBehaviourType.Random;
}

public class ForceTargetBehaviourBuff : ABuff<ForceTargetBehaviourBuffData>
{
    // Given back when the buff ends
    TargetBehaviourType _previousType;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        ITargetProvider targetProvider = target.GetComponent<ITargetProvider>();
        if (targetProvider != null)
        {
            _previousType = targetProvider.targetBehaviourType;
            targetProvider.targetBehaviourType = data.targetBehaviourType;
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        ITargetProvider targetProvider = target != null ? target.GetComponent<ITargetProvider>() : null;
        if (targetProvider != null)
        {
            targetProvider.targetBehaviourType = _previousType;
        }
    }
}
