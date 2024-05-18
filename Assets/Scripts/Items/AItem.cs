using System;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AItemFactory : SerializedScriptableObject
{
    public abstract AItem GetItem();
    public abstract string title { get; }
}

public class ItemFactory<ItemType, DataType> : AItemFactory
                                            where ItemType : AItem<DataType>, new()
                                            where DataType : BaseItemData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override AItem GetItem()
    {
        return new ItemType() { data = this.data };
    }

    public override string title => data != null ? data.name : "None";
}

[Serializable]
public class BaseItemData
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public Sprite icon;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string name;

    [VerticalGroup("Split/Data")]
    [LabelWidth(100)]
    public string description;
}

public abstract class AItem
{
    public abstract void Equip(GameObject target);
    public abstract void Unequip(GameObject target);
    public abstract string title { get; }
    public abstract Sprite icon { get; }
}

public abstract class AItem<DataType> : AItem where DataType : BaseItemData
{
    public DataType data;
    public override string title => data.name;
    public override Sprite icon => data.icon;
}