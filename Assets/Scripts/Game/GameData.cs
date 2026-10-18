using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/GameData")]
public class GameData : ScriptableObject
{
    [Serializable]
    public struct AttributeUpgradeData
    {
        public int startingUpgradeCost;
        public float costIncreaseFactor;
        public float bonusPerUpgrade;
    }

    // Waves that can be fought in a room of this type, between these floors (included)
    [Serializable]
    public class WavePool
    {
        public MapNodeType roomType = MapNodeType.Combat;
        public int minFloor = 0;
        public int maxFloor = 0;
        public List<WavePatternData> wavePatterns = new List<WavePatternData>();
    }

    // Difficulty levels the characters unlock by winning a run
    public AscensionSettings ascension;

    public float playerItemChance = 0.2f;

    // Chance for each reward choice to be a unit, the other choices being items. 0 for now: only the elites give units
    [Range(0f, 1f)]
    public float unitRewardChance = 0.25f;

    // How the items with some tags (e.g. cursed, ascension) stand out in the inventories
    public ItemStyleSet itemStyles;

    // Each unit waits a random part of its attack cooldown, between none and all of it, before its first shot of a
    // fight, so they don't all shoot at once when it starts
    public bool staggerFirstAttacks = true;

    [SerializeField]
    public Dictionary<AttributeType, AttributeUpgradeData> attributeUpgradeData = new Dictionary<AttributeType, AttributeUpgradeData>();

    public List<WavePool> wavePools = new List<WavePool>();

    [HideInlineEditor]
    public List<CharacterData> characters = new List<CharacterData>();

    [HideInlineEditor]
    public List<AItemFactory> items = new List<AItemFactory>();

    // Units found by their tags (e.g. the ones a class can recruit)
    [HideInlineEditor]
    public List<EntityData> entities = new List<EntityData>();

    public List<GameplayTag> tags = new List<GameplayTag>();

    // Every status, found by its title by the {status:X} keywords of the descriptions (see StatusKeywords)
    public List<StatusData> statuses = new List<StatusData>();
}
