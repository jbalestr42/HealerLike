using System;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ATargetValidatorFactory : SerializedScriptableObject
{
    public abstract ATargetValidator GetTargetValidator();
}

public class TargetValidatorFactory<TargetValidatorType, DataType> : ATargetValidatorFactory where TargetValidatorType : ATargetValidator<DataType>, new()
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ATargetValidator GetTargetValidator()
    {
        return new TargetValidatorType() { data = this.data };
    }
}

public abstract class ATargetValidator
{
    public abstract bool IsValid(GameObject target);
}

public abstract class ATargetValidator<DataType> : ATargetValidator
{
    public DataType data;
}