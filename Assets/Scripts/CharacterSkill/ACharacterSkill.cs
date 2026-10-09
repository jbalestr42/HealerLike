using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oisif.Inspector;

[InlineEditor]
public abstract class ACharacterSkillFactory : Sirenix.OdinInspector.SerializedScriptableObject
{
    public abstract ACharacterSkill Create();
}

public class CharacterSkillFactory<CharacterSkillType, DataType> : ACharacterSkillFactory
                                            where CharacterSkillType : ACharacterSkill<DataType>, new()
                                            where DataType : CharacterSkillData, new()
{
    public DataType data;

    public override ACharacterSkill Create()
    {
        return new CharacterSkillType() { data = this.data };
    }
}

public abstract class ACharacterSkill
{
    public abstract CharacterSkillData GetData();
    public abstract void Use(GameObject source, UnityAction<bool> onSkillComplete);
}

[Serializable]
public class CharacterSkillData
{
    [Preview(75)]
    public Sprite icon;

    public string name;

    [TextArea(10, 10)]
    [InfoBox(nameof(GetDescriptionPreview))]
    public string description;

    // The description as the player reads it, shown above it in the inspector
    string GetDescriptionPreview() => TextConvertor.Convert(description, null, this);

    [CreateDataButton]
    public List<ACharacterSkillValidatorFactory> validators;
}

public abstract class ACharacterSkill<DataType> : ACharacterSkill where DataType : CharacterSkillData
{
    public DataType data;
    public override CharacterSkillData GetData() => data;

}