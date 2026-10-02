using System.Collections.Generic;
using UnityEngine.Events;

public class InventoryItemData
{
    public AItem item;
    public int inventoryIndex = -1;
    public bool isValidIndex => inventoryIndex >= 0;
}

public class InventoryHandler
{
    List<InventoryItemData> _items = new List<InventoryItemData>();
    public List<InventoryItemData> items => _items;

    public UnityEvent<InventoryItemData, bool> OnItemAdded = new UnityEvent<InventoryItemData, bool>();
    public UnityEvent<InventoryItemData> OnItemRemoved = new UnityEvent<InventoryItemData>();

    public void AddItem(AItem item, int inventoryIndex, bool isNewItem = true)
    {
        InventoryItemData itemData = new InventoryItemData() { item = item, inventoryIndex = inventoryIndex };
        _items.Add(itemData);
        OnItemAdded.Invoke(itemData, isNewItem);
    }

    public void RemoveItem(AItem item)
    {
        InventoryItemData itemData = _items.Find(itemData => itemData.item == item);
        _items.Remove(itemData);
        OnItemRemoved.Invoke(itemData);
    }

    // Lowest inventory index not used by any item
    public int GetFirstFreeIndex()
    {
        int index = 0;
        while (_items.Exists(itemData => itemData.inventoryIndex == index))
        {
            index++;
        }
        return index;
    }

    public void TransfertItem(AItem item, int inventoryIndex, InventoryHandler inventoryHandler)
    {
        inventoryHandler.RemoveItem(item);
        AddItem(item, inventoryIndex, false);
    }

    // Every item of the other inventory moves to this one, each in the first free slot (e.g. the items of a
    // dead unit going back to the player)
    public void TakeAllItems(InventoryHandler inventoryHandler)
    {
        List<InventoryItemData> itemsToTake = new List<InventoryItemData>(inventoryHandler.items);
        foreach (InventoryItemData itemData in itemsToTake)
        {
            inventoryHandler.RemoveItem(itemData.item);
            AddItem(itemData.item, -1);
        }
    }

    public void TransfertItems(InventoryHandler inventoryHandler)
    {
        List<InventoryItemData> itemsToRemove = new List<InventoryItemData>();
        itemsToRemove.AddRange(inventoryHandler.items);

        foreach (InventoryItemData itemData in itemsToRemove)
        {
            inventoryHandler.RemoveItem(itemData.item);
            AddItem(itemData.item, itemData.inventoryIndex);
        }
    }
}