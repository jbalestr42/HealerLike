using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/EntityData")]
[InlineEditor]
public class EntityData : SerializedScriptableObject
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
    public string description;

    [Space]
    [HorizontalGroup("Group")]
    [VerticalGroup("Group/Attributes")]
    [DictionaryDrawerSettings(KeyColumnWidth = 75f, KeyLabel = "Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [VerticalGroup("Group/Target")]
    public TargetBehaviourType targetBehaviourType;

    [VerticalGroup("Group/Target")]
    public List<ATargetValidatorFactory> targetValidators;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(passives)")]
    public List<ABuffHandlerFactory> passives;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(onHitEffects)")]
    public List<ABuffHandlerFactory> onHitEffects;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillFactory>, ASkillFactory>(skillFactories)")]
    public List<ASkillFactory> skillFactories;
}