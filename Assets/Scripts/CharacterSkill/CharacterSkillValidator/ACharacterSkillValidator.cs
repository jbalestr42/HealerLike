using UnityEngine;
using Oisif.Inspector;

[InlineEditor]
public abstract class ACharacterSkillValidatorFactory : ScriptableObject
{
    public abstract ACharacterSkillValidator Create();
}

public class CharacterSkillValidatorFactory<CharacterSkillValidatorType, DataType> : ACharacterSkillValidatorFactory, IGameDataSource where CharacterSkillValidatorType : ACharacterSkillValidator<DataType>, new()
{
    [InlineProperty]
    public DataType data;
    public object sourceData { get { return data; } }

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
    // Only gives something when the skill is used (e.g. the Devotion of a heal), never blocking it: still applied to
    // the skills without restriction (the sandbox)
    public virtual bool IsGain(GameObject owner) => false;
}

public abstract class ACharacterSkillValidator<DataType> : ACharacterSkillValidator, IGameDataSource
{
    public DataType data;
    public object sourceData { get { return data; } }
}