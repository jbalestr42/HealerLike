using System.Collections.Generic;
using UnityEngine;

public class TargetRequirement : IRequirement
{
    ITargetProvider _targetProvider;

    public TargetRequirement(GameObject source)
    {
        _targetProvider = source.GetComponent<ITargetProvider>();
    }

    public bool IsValid(GameObject source)
    {
        List<GameObject> targets = _targetProvider.GetTargets();
        return targets != null && targets.Count > 0;
    }
}