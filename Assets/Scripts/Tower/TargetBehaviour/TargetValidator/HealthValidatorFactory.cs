using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/HealthValidator")]
public class HealthValidatorFactory : TargetValidatorFactory<HealthValidator, HealthValidatorData> {}

[Serializable]
public class HealthValidatorData
{
    public float threshold;
}

public class HealthValidator : ATargetValidator<HealthValidatorData>
{
    public override bool IsValid(GameObject target)
    {
        return target.GetComponent<Entity>().health.percent <= data.threshold;
    }
}