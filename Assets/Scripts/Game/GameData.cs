using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/GameData")]
public class GameData : SerializedScriptableObject
{
    [InlineProperty(LabelWidth=130)]
    public struct AttributeUpgradeData
    {
        public int startingUpgradeCost;
        public float costIncreaseFactor;
        public float bonusPerUpgrade;
    }

    [HorizontalGroup("Split")]
    [BoxGroup("Split/Player Data")]
    public int gold = 100;

    [BoxGroup("Split/Player Data")]
    public int life = 20;

    [BoxGroup("Split/Player Data")]
    public float refoundFactor = 0.75f;

    [BoxGroup("Split/Upgrade Data")]
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout)]
    public Dictionary<AttributeType, AttributeUpgradeData> attributeUpgradeData = new Dictionary<AttributeType, AttributeUpgradeData>();

    public List<AWaveData> waves = new List<AWaveData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<WavePatternData>, WavePatternData>(wavePatterns, this)")]
    public List<WavePatternData> wavePatterns = new List<WavePatternData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<EnemyData>, EnemyData>(enemies, this)")]
    public List<EnemyData> enemies = new List<EnemyData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<TowerData>, TowerData>(towers, this)")]
    public List<TowerData> towers = new List<TowerData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<EntityData>, EntityData>(entities, this)")]
    public List<EntityData> entities = new List<EntityData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<CharacterData>, CharacterData>(characters, this)")]
    public List<CharacterData> characters = new List<CharacterData>();

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.DrawRefreshButton<List<AItemFactory>, AItemFactory>(items, this)")]
    public List<AItemFactory> items = new List<AItemFactory>();
}