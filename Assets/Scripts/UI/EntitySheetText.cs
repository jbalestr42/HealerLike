using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Texts of the detail sheets of a unit and of the healer, kept apart from the UI so they can be tested
public static class EntitySheetText
{
    // The items the entity is born with
    public static List<string> GetPassiveLines(Entity entity)
    {
        List<string> lines = new List<string>();
        foreach (AItem item in entity.items)
        {
            lines.Add(EntityInfoFormatter.FormatItemDetails(item));
        }
        return lines;
    }

    // The items the player put in the inventory of the entity
    public static List<string> GetEquippedItemLines(Entity entity)
    {
        List<string> lines = new List<string>();
        foreach (InventoryItemData itemData in entity.inventoryHandler.items)
        {
            lines.Add(EntityInfoFormatter.FormatItemDetails(itemData.item));
        }
        return lines;
    }

    // The healer's sheet: mana, stats, items and active effects
    public static string BuildCharacterBody(Character character)
    {
        StringBuilder builder = new StringBuilder();
        GameObject owner = character.gameObject;

        string title = character.data != null ? character.data.title : owner.name;
        builder.Append($"<size=+6><b>{title}</b></size>\n");
        EntityInfoFormatter.AppendSection(builder, "Mana", GetManaLines(character));
        EntityInfoFormatter.AppendSection(builder, "Stats", EntityInfoFormatter.GetAttributeLines(character.attributeManager, owner));
        EntityInfoFormatter.AppendSection(builder, "Items", GetItemLines(character));
        EntityInfoFormatter.AppendSection(builder, "Active effects", character.buffManager != null ? EntityInfoFormatter.GetBuffLines(character.buffManager, owner) : new List<string>());
        return builder.ToString().TrimEnd();
    }

    static List<string> GetManaLines(Character character)
    {
        List<string> lines = new List<string>();
        if (character.mana != null)
        {
            lines.Add($"<b>{EntityInfoFormatter.FormatNumber(character.mana.Value)} / {EntityInfoFormatter.FormatNumber(character.mana.Max)}</b>");
        }
        return lines;
    }

    // The items the character starts with, then the ones won during the run
    static List<string> GetItemLines(Character character)
    {
        List<string> lines = new List<string>();
        foreach (AItem item in character.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(item, true));
        }
        foreach (InventoryItemData itemData in character.inventoryHandler.items)
        {
            lines.Add(EntityInfoFormatter.FormatItem(itemData.item, false));
        }
        return lines;
    }
}
