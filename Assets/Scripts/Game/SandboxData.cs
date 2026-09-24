using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/SandboxData")]
public class SandboxData : SerializedScriptableObject
{
    // Attributes (mana, ...) of the sandbox character are taken from this character
    public CharacterData character;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<EntityData>, EntityData>(entities, this)")]
    public List<EntityData> entities = new List<EntityData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<ACharacterSkillFactory>, ACharacterSkillFactory>(characterSkills, this)")]
    public List<ACharacterSkillFactory> characterSkills = new List<ACharacterSkillFactory>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<AItemFactory>, AItemFactory>(items, this)")]
    public List<AItemFactory> items = new List<AItemFactory>();

    public CharacterData CreateCharacterData()
    {
        CharacterData characterData = CreateInstance<CharacterData>();
        characterData.name = "SandboxCharacter";
        characterData.model = character.model;
        characterData.title = "Sandbox";
        characterData.attributes = new Dictionary<AttributeType, float>(character.attributes);
        characterData.passives = new List<ABuffHandlerFactory>();
        characterData.entities = new List<EntityData>();
        characterData.skills = new List<ACharacterSkillFactory>(characterSkills);
        return characterData;
    }
}
