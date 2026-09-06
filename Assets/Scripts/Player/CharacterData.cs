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
    [SerializeField]
    [DictionaryDrawerSettings(KeyLabel = "Attribute Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(passives)")]
    public List<ABuffHandlerFactory> passives;

    [Space]
    public List<EntityData> entities = new List<EntityData>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ACharacterSkillFactory>, ACharacterSkillFactory>(skills)")]
    public List<ACharacterSkillFactory> skills = new List<ACharacterSkillFactory>();
}