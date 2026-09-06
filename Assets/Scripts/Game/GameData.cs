using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/GameData")]
public class GameData : SerializedScriptableObject
{
    [Serializable]
    [InlineProperty(LabelWidth = 130)]
    public struct AttributeUpgradeData
    {
        public int startingUpgradeCost;
        public float costIncreaseFactor;
        public float bonusPerUpgrade;
    }

    [Serializable]
    public class WavePerRound
    {
        public int round;
        public List<WavePatternData> wavePatterns = new List<WavePatternData>();
    }

    [HorizontalGroup("Split")]
    [BoxGroup("Split/Player Data")]
    public int gold = 100;

    [HorizontalGroup("Split")]
    [BoxGroup("Split/Player Data")]
    public float playerItemChance = 0.2f;

    [BoxGroup("Split/Upgrade Data")]
    [SerializeField]
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout)]
    public Dictionary<AttributeType, AttributeUpgradeData> attributeUpgradeData = new Dictionary<AttributeType, AttributeUpgradeData>();

    [BoxGroup("Split/Player Data")]
    public List<WavePerRound> wavePerRound = new List<WavePerRound>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<CharacterData>, CharacterData>(characters, this)")]
    public List<CharacterData> characters = new List<CharacterData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<AItemFactory>, AItemFactory>(items, this)")]
    public List<AItemFactory> items = new List<AItemFactory>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<AItemFactory>, AItemFactory>(playerItems, this)")]
    public List<AItemFactory> playerItems = new List<AItemFactory>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<GameplayTag>, GameplayTag>(tags, this)")]
    public List<GameplayTag> tags = new List<GameplayTag>();
}