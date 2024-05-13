using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/EntityData")]
[InlineEditor]
public class EntityData : SerializedScriptableObject
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
    public string text;

    [VerticalGroup("Data/Stats")]
    [LabelWidth(100)]
    public int price;

    [Space]
    [DictionaryDrawerSettings(KeyLabel = "Attribute Type", ValueLabel = "Value")]
    public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffFactory>, ABuffFactory>(passives)")]
    public List<ABuffFactory> passives;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(onHitEffects)")]
    public List<ABuffHandlerFactory> onHitEffects;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AConsumerFactory>, AConsumerFactory>(onHitConsumer)")]
    public List<AConsumerFactory> onHitConsumer;

    [Space]
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ASkillFactory>, ASkillFactory>(skillFactories)")]
    public List<ASkillFactory> skillFactories;
}