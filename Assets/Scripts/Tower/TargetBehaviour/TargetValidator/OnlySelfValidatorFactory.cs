using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/OnlySelfValidator")]
public class OnlySelfValidatorFactory : TargetValidatorFactory<OnlySelfValidator, OnlySelfValidatorData> {}

[Serializable]
public class OnlySelfValidatorData
{
}

public class OnlySelfValidator : ATargetValidator<OnlySelfValidatorData>
{
    public override bool IsValid(GameObject source, GameObject target)
    {
        return source == target;
    }
}