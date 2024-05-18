using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterData")]
[InlineEditor]
public class CharacterData : SerializedScriptableObject
{
    [HorizontalGroup("Data", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public GameObject model;

    [VerticalGroup("Data/Stats")]
    [LabelWidth(100)]
    public string title;

    [VerticalGroup("Data/Stats")]
    [LabelWidth(100)]
    public string text;

    [Space]
    [DictionaryDrawerSettings(KeyLabel = "Attribute Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(passives)")]
    public List<ABuffFactory> passives;

    public List<CharacterSkillSlotData> skills = new List<CharacterSkillSlotData>();
}