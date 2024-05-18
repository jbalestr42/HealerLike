using System;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class ACharacterSkillValidatorFactory : SerializedScriptableObject
{
    public abstract ACharacterSkillValidator Create();
}

public class CharacterSkillValidatorFactory<CharacterSkillValidatorType, DataType> : ACharacterSkillValidatorFactory where CharacterSkillValidatorType : ACharacterSkillValidator<DataType>, new()
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ACharacterSkillValidator Create()
    {
        return new CharacterSkillValidatorType() { data = this.data };
    }
}

public abstract class ACharacterSkillValidator
{
    public virtual void Init(UseCharacterSkillButton skillButton, GameObject owner) {}
    public virtual void Update(UseCharacterSkillButton skillButton) {}
    public abstract bool IsValid(GameObject owner);
    public abstract void OnSkillUsed(GameObject owner);
}

public abstract class ACharacterSkillValidator<DataType> : ACharacterSkillValidator
{
    public DataType data;
}