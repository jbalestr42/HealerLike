
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class CharacterSkillSlotData
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

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ACharacterSkillValidatorFactory>, ACharacterSkillValidatorFactory>(validators)")]
    public List<ACharacterSkillValidatorFactory> validators;

    [CreateDataButton]
    public ACharacterSkillFactory skill;

}