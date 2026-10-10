using System;
using UnityEngine;
using Oisif.Inspector;

[InlineEditor]
public abstract class ATargetValidatorFactory : ScriptableObject
{
    public abstract ATargetValidator GetTargetValidator();
}

public class TargetValidatorFactory<TargetValidatorType, DataType> : ATargetValidatorFactory where TargetValidatorType : ATargetValidator<DataType>, new()
{
    [InlineProperty]
    public DataType data;

    public override ATargetValidator GetTargetValidator()
    {
        return new TargetValidatorType() { data = this.data };
    }
}

public abstract class ATargetValidator
{
    public abstract bool IsValid(GameObject source, GameObject target);
}

public abstract class ATargetValidator<DataType> : ATargetValidator
{
    public DataType data;
}