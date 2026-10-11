using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/EntityData")]
[InlineEditor]
public class EntityData : ScriptableObject, ITaggable, IInspectorPreview
{
    [Preview(75)]
    public GameObject model;

    public string title;

    public string description;

    [Space]
    [SerializeField]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    public TargetBehaviourType targetBehaviourType;

    public List<ATargetValidatorFactory> targetValidators;

    public List<GameplayTag> tags = new List<GameplayTag>();

    [Space]
    [CreateDataButton]
    public List<AItemFactory> items = new List<AItemFactory>();

    [Space]
    [CreateDataButton]
    public List<ASkillFactory> skillFactories;

    #region IInspectorPreview

    public Object previewObject => model;
    public string previewLabel => title;

    #endregion

    #region ITaggable

    public bool HasTag(GameplayTag tag) => TagFilter.HasTag(tags, tag);
    public bool HasTag(string tagName) => TagFilter.HasTag(tags, tagName);

    #endregion
}
