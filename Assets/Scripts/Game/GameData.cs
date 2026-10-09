using System;
using System.Collections.Generic;
using UnityEngine;

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

    public float playerItemChance = 0.2f;

    // Chance for each reward choice to be a unit, the other choices being items
    [Range(0f, 1f)]
    public float unitRewardChance = 0.25f;

    [SerializeField]
    public Dictionary<AttributeType, AttributeUpgradeData> attributeUpgradeData = new Dictionary<AttributeType, AttributeUpgradeData>();

    public List<WavePool> wavePools = new List<WavePool>();

    // Each unit waits a random part of its attack cooldown, between none and all of it, before its first shot of a
    // fight, so they don't all shoot at once when it starts
    public bool staggerFirstAttacks = true;

    public List<CharacterData> characters = new List<CharacterData>();

    public List<AItemFactory> items = new List<AItemFactory>();

    // Units found by their tags (e.g. the ones a class can recruit)
    public List<EntityData> entities = new List<EntityData>();

    public List<GameplayTag> tags = new List<GameplayTag>();
}