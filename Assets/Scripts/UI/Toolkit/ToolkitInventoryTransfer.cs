using System.Collections.Generic;

namespace HealerLike.UI.Toolkit
{
    /// <summary>Preserves equipment slot identity, which determines stacking strength in Entity.</summary>
    public static class ToolkitInventoryTransfer
    {
        public static int FirstEmptySlot(InventoryHandler inventory)
        {
            int index = 0;
            while (inventory.items.Exists(item => item.inventoryIndex == index)) index++;
            return index;
        }

        public static List<int> EmptySlots(InventoryHandler inventory)
        {
            var slots = new List<int>();
            for (int index = 0; index < System.Math.Max(3, inventory.items.Count + 1); index++)
                if (!inventory.items.Exists(item => item.inventoryIndex == index)) slots.Add(index);
            return slots;
        }

        public static bool Transfer(AItem item, InventoryHandler source, InventoryHandler destination, int index)
        {
            if (item == null || source == null || destination == null || source == destination || index < 0 ||
                !source.items.Exists(entry => entry.item == item) || destination.items.Exists(entry => entry.inventoryIndex == index))
                return false;
            source.RemoveItem(item);
            // The explicit index is authoritative; this is a move, not a new reward.
            destination.AddItem(item, index, false);
            return true;
        }
    }
}
