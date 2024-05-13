using System.Collections;
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
        return _targetProvider.GetTargets().Count > 0;
    }
}