using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public abstract class ACharacterSkillFactory : SerializedScriptableObject
{
    public abstract ACharacterSkill Create();
}

public class CharacterSkillFactory<CharacterSkillType, DataType> : ACharacterSkillFactory
                                            where CharacterSkillType : ACharacterSkill<DataType>, new()
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ACharacterSkill Create()
    {
        return new CharacterSkillType() { data = this.data };
    }
}

public abstract class ACharacterSkill
{
    public abstract void Use(GameObject source, UnityAction<bool> onSkillComplete);
}

public abstract class ACharacterSkill<DataType> : ACharacterSkill
{
    public DataType data;
}