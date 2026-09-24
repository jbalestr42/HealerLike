using System.Collections.Generic;
using UnityEngine;

// Moves equipment between inventories and keeps the slot index, which sets the stacking strength in Entity
public static class ToolkitInventoryTransfer
{
    public static int FirstEmptySlot(InventoryHandler inventory)
    {
        int index = 0;
        while (IsOccupied(inventory, index))
        {
            index++;
        }

        return index;
    }

    public static List<int> EmptySlots(InventoryHandler inventory)
    {
        List<int> slots = new List<int>();
        for (int index = 0; index < Mathf.Max(3, inventory.items.Count + 1); index++)
        {
            if (!IsOccupied(inventory, index))
            {
                slots.Add(index);
            }
        }

        return slots;
    }

    public static bool Transfer(AItem item, InventoryHandler source, InventoryHandler destination, int index)
    {
        if (item == null || source == null || destination == null || source == destination || index < 0)
        {
            return false;
        }

        if (!Contains(source, item) || IsOccupied(destination, index))
        {
            return false;
        }

        source.RemoveItem(item);
        // The explicit index is authoritative: this is a move, not a new reward
        destination.AddItem(item, index, false);
        return true;
    }

    public static bool Contains(InventoryHandler inventory, AItem item)
    {
        foreach (InventoryItemData entry in inventory.items)
        {
            if (entry.item == item)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsOccupied(InventoryHandler inventory, int index)
    {
        foreach (InventoryItemData entry in inventory.items)
        {
            if (entry.inventoryIndex == index)
            {
                return true;
            }
        }

        return false;
    }
}
