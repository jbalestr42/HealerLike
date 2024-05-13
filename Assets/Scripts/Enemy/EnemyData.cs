using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/EnemyData")]
[InlineEditor]
public class EnemyData : SerializedScriptableObject
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
    public int income;

    [VerticalGroup("Data/Stats")]
    [LabelWidth(100)]
    public int lifeCost;

    [VerticalGroup("Data/Stats")]
    [LabelWidth(100)]
    public EnemyRank rank;

    [Space]
    [DictionaryDrawerSettings(KeyLabel = "Attribute Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(passives)")]
    public List<ABuffFactory> passives;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillFactory>, ASkillFactory>(skillFactories)")]
    public List<ASkillFactory> skillFactories;
}