using System.Collections.Generic;
using UnityEngine;

// Reconciles the legacy inventory views with items moved by the Toolkit, through their public API
public static class LegacyInventoryAdapter
{
    public static void Synchronize(InventoryHandler owner)
    {
        if (owner == null)
        {
            return;
        }

        SlotInventory[] inventories = Object.FindObjectsByType<SlotInventory>(FindObjectsInactive.Include);
        foreach (SlotInventory inventory in inventories)
        {
            if (inventory.inventoryHandler == owner)
            {
                Refresh(inventory);
            }
        }
    }

    public static void Refresh(SlotInventory inventory)
    {
        if (inventory == null || inventory.inventoryHandler == null)
        {
            return;
        }

        List<SlotInventoryItem> slots = new List<SlotInventoryItem>();
        foreach (Transform child in inventory.transform)
        {
            SlotInventoryItem slot = child.GetComponent<SlotInventoryItem>();
            if (slot == null)
            {
                continue;
            }

            ClearSlot(slot);
            slot.index = slots.Count;
            slots.Add(slot);
        }

        int requiredSlots = 0;
        List<InventoryItemData> items = inventory.inventoryHandler.items;
        for (int i = 0; i < items.Count; i++)
        {
            int index = items[i].isValidIndex ? items[i].inventoryIndex : i;
            requiredSlots = Mathf.Max(requiredSlots, index + 1);
        }

        if (requiredSlots > 0 && slots.Count == 0)
        {
            slots.Add(inventory.GetEmptySlot());
        }

        // RefreshInventory rebuilds its slot list from these direct children.
        // Clone an empty slot so the serialized visuals and the drag and drop behaviour stay intact
        while (slots.Count < requiredSlots)
        {
            SlotInventoryItem slot = Object.Instantiate(slots[0], inventory.transform);
            slot.index = slots.Count;
            slot.inventoryHandler = inventory.inventoryHandler;
            slots.Add(slot);
        }

        inventory.RefreshInventory();
    }

    static void ClearSlot(SlotInventoryItem slot)
    {
        slot.inventoryItem = null;
        for (int i = slot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = slot.transform.GetChild(i);
            // Detach right away: the legacy slot allocation reads childCount this frame
            child.SetParent(null);
            child.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Object.Destroy(child.gameObject);
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
