using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/IgnoreSelfValidator")]
public class IgnoreSelfValidatorFactory : TargetValidatorFactory<IgnoreSelfValidator, IgnoreSelfValidatorData> {}

[Serializable]
public class IgnoreSelfValidatorData
{
}

public class IgnoreSelfValidator : ATargetValidator<IgnoreSelfValidatorData>
{
    public override bool IsValid(GameObject source, GameObject target)
    {
        return source != target;
    }
}