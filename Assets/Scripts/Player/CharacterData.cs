using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/CharacterData")]
[InlineEditor]
public class CharacterData : ScriptableObject
{
    [Preview(75)]
    public GameObject model;

    public string title;

    public string text;

    [Space]
    [SerializeField]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [Space]
    [CreateDataButton]
    public List<AItemFactory> items = new List<AItemFactory>();

    [Space]
    public List<EntityData> entities = new List<EntityData>();

    // The units tagged with it and Reward can be recruited by the character (rewards, recruitment event)
    public GameplayTag classTag;

    [Space]
    [CreateDataButton]
    public List<ACharacterSkillFactory> skills = new List<ACharacterSkillFactory>();
}