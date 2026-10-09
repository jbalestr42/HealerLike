using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

public abstract class AItemFactory : Sirenix.OdinInspector.SerializedScriptableObject, ITaggable
{
    public abstract AItem GetItem();
    public abstract string title { get; }
    public abstract List<GameplayTag> tags { get; }

    #region ITaggable

    public bool HasTag(GameplayTag tag) => TagFilter.HasTag(tags, tag);
    public bool HasTag(string tagName) => TagFilter.HasTag(tags, tagName);

    #endregion
}

public class ItemFactory<ItemType, DataType> : AItemFactory
                                            where ItemType : AItem<DataType>, new()
                                            where DataType : BaseItemData
{
    public DataType data;

    public override AItem GetItem()
    {
        return new ItemType() { data = this.data };
    }

    public override string title => data != null ? data.name : "None";
    public override List<GameplayTag> tags => data != null ? data.tags : new List<GameplayTag>();
}

[Serializable]
public class BaseItemData
{
    [Preview(75)]
    public Sprite icon;

    public string name;

    public string description;

    public List<GameplayTag> tags = new List<GameplayTag>();
}

public abstract class AItem : ITaggable
{
    public abstract void Equip(GameObject target);
    public abstract void Unequip(GameObject target);
    public abstract string title { get; }
    public abstract string description { get; }
    public abstract Sprite icon { get; }
    public abstract List<GameplayTag> tags { get; }

    #region ITaggable

    public bool HasTag(GameplayTag tag) => TagFilter.HasTag(tags, tag);
    public bool HasTag(string tagName) => TagFilter.HasTag(tags, tagName);

    #endregion
}

public abstract class AItem<DataType> : AItem where DataType : BaseItemData
{
    public DataType data;
    public override string title => data.name;
    public override string description => data.description;
    public override Sprite icon => data.icon;
    public override List<GameplayTag> tags => data.tags;
}