using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/InvincibleValidator")]
public class InvincibleValidatorFactory : TargetValidatorFactory<InvincibleValidator, InvincibleValidatorData> {}

[Serializable]
public class InvincibleValidatorData
{
    public bool inverse;
}

public class InvincibleValidator : ATargetValidator<InvincibleValidatorData>
{
    public override bool IsValid(GameObject target)
    {
        return target.GetComponent<Entity>().health.preventConsumers == data.inverse;
    }
}