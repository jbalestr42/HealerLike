using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/SandboxData")]
public class SandboxData : SerializedScriptableObject
{
    // Attributes (mana, ...) of the sandbox character are taken from this character
    public CharacterData character;

    // Characters that can be played in the sandbox, with their own skills and items
    public List<CharacterData> characters = new List<CharacterData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<EntityData>, EntityData>(entities, this)")]
    public List<EntityData> entities = new List<EntityData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<WavePatternData>, WavePatternData>(waves, this)")]
    public List<WavePatternData> waves = new List<WavePatternData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<ACharacterSkillFactory>, ACharacterSkillFactory>(characterSkills, this)")]
    public List<ACharacterSkillFactory> characterSkills = new List<ACharacterSkillFactory>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<AItemFactory>, AItemFactory>(items, this)")]
    public List<AItemFactory> items = new List<AItemFactory>();

    // played: null plays with every sandbox skill, otherwise the character with its skills and items
    public CharacterData CreateCharacterData(CharacterData played = null)
    {
        CharacterData source = played != null ? played : character;
        CharacterData characterData = CreateInstance<CharacterData>();
        characterData.name = "SandboxCharacter";
        characterData.model = source.model;
        characterData.title = played != null ? played.title : "Sandbox";
        characterData.attributes = new Dictionary<AttributeType, float>(source.attributes);
        characterData.items = played != null && played.items != null ? new List<AItemFactory>(played.items) : new List<AItemFactory>();
        // The sandbox units are placed from the sandbox panel
        characterData.entities = new List<EntityData>();
        characterData.skills = new List<ACharacterSkillFactory>(played != null ? played.skills : characterSkills);
        return characterData;
    }

    // Every skill mode (null), then each character, then back to every skill
    public CharacterData GetNextCharacter(CharacterData current)
    {
        int index = current != null ? characters.IndexOf(current) : -1;
        return index + 1 < characters.Count ? characters[index + 1] : null;
    }
}
