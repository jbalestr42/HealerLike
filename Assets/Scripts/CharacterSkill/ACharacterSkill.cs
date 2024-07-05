using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[InlineEditor]
public abstract class ACharacterSkillFactory : SerializedScriptableObject
{
    public abstract ACharacterSkill Create();
}

public class CharacterSkillFactory<CharacterSkillType, DataType> : ACharacterSkillFactory
                                            where CharacterSkillType : ACharacterSkill<DataType>, new()
                                            where DataType : CharacterSkillData, new()
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
    public abstract CharacterSkillData GetData();
    public abstract void Use(GameObject source, UnityAction<bool> onSkillComplete);
}

[Serializable]
public class CharacterSkillData
{
    [TitleGroup("Common")]
    [HorizontalGroup("Common/Split")]
    [VerticalGroup("Common/Split/Left")]
    [BoxGroup("Common/Split/Left/Displayed Data")]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public Sprite icon;

    [BoxGroup("Common/Split/Left/Displayed Data")]
    public string name;

    [BoxGroup("Common/Split/Left/Displayed Data")]
    [TextArea(4, 10)]
    public string description;

    [VerticalGroup("Common/Split/Right")]
    [BoxGroup("Common/Split/Right/Validators")]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ACharacterSkillValidatorFactory>, ACharacterSkillValidatorFactory>(validators)")]
    public List<ACharacterSkillValidatorFactory> validators;
}

public abstract class ACharacterSkill<DataType> : ACharacterSkill where DataType : CharacterSkillData
{
    public DataType data;
    public override CharacterSkillData GetData() => data;

}