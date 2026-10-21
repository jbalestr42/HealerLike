using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ToolkitPreviewData
{
    static readonly int cardCount = 8;
    static readonly int rewardCount = 3;

    public static void FillCards(ToolkitGameView view)
    {
        List<ToolkitCardModel> creatures = new List<ToolkitCardModel>();
        foreach (EntityData data in FindAssets<EntityData>())
        {
            creatures.Add(CreateCard(data, data.title, data.description));
        }

        List<ToolkitCardModel> spells = new List<ToolkitCardModel>();
        foreach (ACharacterSkillFactory data in FindAssets<ACharacterSkillFactory>())
        {
            spells.Add(CreateCard(data, data.name, "Inspect spell details in the game."));
        }

        List<ToolkitCardModel> items = new List<ToolkitCardModel>();
        foreach (AItemFactory data in FindAssets<AItemFactory>())
        {
            items.Add(CreateCard(data, data.title, "Equipment reward"));
        }

        view.SetCards("party-list", creatures);
        view.SetCards("spell-list", spells);
        view.SetCards("inventory-list", items);
        view.SetCards("upgrade-list", items.GetRange(0, Mathf.Min(rewardCount, items.Count)));
        if (creatures.Count > 0)
        {
            view.ShowDetail(creatures[0]);
        }
    }

    static List<DataType> FindAssets<DataType>()
        where DataType : ScriptableObject
    {
        List<DataType> assets = new List<DataType>();
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(DataType).Name);
        for (int i = 0; i < guids.Length && i < cardCount; i++)
        {
            DataType data = AssetDatabase.LoadAssetAtPath<DataType>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (data != null)
            {
                assets.Add(data);
            }
        }

        return assets;
    }

    static ToolkitCardModel CreateCard(Object data, string title, string description)
    {
        ToolkitCardModel model = new ToolkitCardModel();
        model.iconSource = data;
        model.title = title;
        model.description = description;
        model.status = "Ready";
        return model;
    }
}
