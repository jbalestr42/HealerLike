using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/EntityData")]
[InlineEditor]
public class EntityData : SerializedScriptableObject, ITaggable
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
    [SerializeField]
    [HorizontalGroup("Group")]
    [VerticalGroup("Group/Attributes")]
    [DictionaryDrawerSettings(KeyColumnWidth = 75f, KeyLabel = "Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [VerticalGroup("Group/Target")]
    public TargetBehaviourType targetBehaviourType;

    [VerticalGroup("Group/Target")]
    public List<ATargetValidatorFactory> targetValidators;

    [VerticalGroup("Group/Target")]
    public List<GameplayTag> tags = new List<GameplayTag>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AItemFactory>, AItemFactory>(items)")]
    public List<AItemFactory> items = new List<AItemFactory>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillFactory>, ASkillFactory>(skillFactories)")]
    public List<ASkillFactory> skillFactories;

    #region ITaggable

    public bool HasTag(GameplayTag tag) => TagFilter.HasTag(tags, tag);
    public bool HasTag(string tagName) => TagFilter.HasTag(tags, tagName);

    #endregion
}
