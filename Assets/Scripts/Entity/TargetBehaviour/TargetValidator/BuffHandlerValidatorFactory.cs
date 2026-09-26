using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/BuffHandlerValidator")]
public class BuffHandlerValidatorFactory : TargetValidatorFactory<BuffHandlerValidator, BuffHandlerValidatorData> {}

[Serializable]
public class BuffHandlerValidatorData
{
    public ABuffHandlerFactory buffHandlerFactory;
    // false: only targets that don't have the handler yet (ex: don't curse twice the same entity)
    public bool mustHave;
}

public class BuffHandlerValidator : ATargetValidator<BuffHandlerValidatorData>
{
    public override bool IsValid(GameObject source, GameObject target)
    {
        return target.GetComponent<BuffManager>().HasHandler(data.buffHandlerFactory) == data.mustHave;
    }
}
