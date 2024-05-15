using System;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class ACharacterSkillFactory : SerializedScriptableObject
{
    public abstract ACharacterSkill GetCharacterSkill();
    public abstract string title { get; }
}

public class CharacterSkillFactory<CharacterSkillType, DataType> : ACharacterSkillFactory
                                            where CharacterSkillType : ACharacterSkill<DataType>, new()
                                            where DataType : BaseCharacterSkillData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ACharacterSkill GetCharacterSkill()
    {
        return new CharacterSkillType() { data = this.data };
    }

    public override string title => data != null ? data.name : "None";
}

[Serializable]
public class BaseCharacterSkillData
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public Sprite icon;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string name;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string description;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public int cost;

    public string commentGererUSage = "cooldown/afterfight/etc.";
}

public abstract class ACharacterSkill
{
    public abstract void Use();
    public abstract string title { get; }
    public abstract Sprite icon { get; }
}

public abstract class ACharacterSkill<DataType> : ACharacterSkill where DataType : BaseCharacterSkillData
{
    public DataType data;
    public override string title => data.name;
    public override Sprite icon => data.icon;
}