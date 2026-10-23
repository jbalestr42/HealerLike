using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The healer's items as icons, the ones it starts with then the ones won: an item that stands out (e.g. cursed,
// ascension) is outlined with the color of its ItemStyle, an item counting toward an effect (e.g. the Rosary) shows
// its count. Hovered or tapped, an icon opens the name, the style and the description of its item in the popover
public sealed class ToolkitItemBar : IDisposable
{
    public const string BarName = "item-bar";

    ToolkitGameContext _context;
    ToolkitKitRow _row;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        Dispose();
        _context = context;
        VisualElement bar = view.root.Q(BarName);
        if (bar != null)
        {
            _row = new ToolkitKitRow(view, bar);
        }
    }

    public void Dispose()
    {
        _row?.Dispose();
        _row = null;
        _context = null;
    }

    public void Refresh(bool show)
    {
        if (_row == null)
        {
            return;
        }

        Character character = _context.player != null ? _context.player.character : null;
        ItemStyleSet styles = _context.data != null ? _context.data.itemStyles : null;
        _row.Refresh(show && character != null ? GetEntries(character, styles) : null);
    }

    public static List<ToolkitKitEntry> GetEntries(Character character, ItemStyleSet styles)
    {
        List<ToolkitKitEntry> entries = new List<ToolkitKitEntry>();
        BuffManager buffManager = character.GetComponent<BuffManager>();
        foreach (AItem item in character.items)
        {
            AddEntry(entries, item, styles, buffManager);
        }

        if (character.inventoryHandler != null)
        {
            foreach (InventoryItemData itemData in character.inventoryHandler.items)
            {
                AddEntry(entries, itemData.item, styles, buffManager);
            }
        }

        return entries;
    }

    static void AddEntry(List<ToolkitKitEntry> entries, AItem item, ItemStyleSet styles, BuffManager buffManager)
    {
        if (item != null)
        {
            entries.Add(GetEntry(item, styles != null ? styles.GetStyle(item) : null, buffManager));
        }
    }

    public static ToolkitKitEntry GetEntry(AItem item, ItemStyle style, BuffManager buffManager)
    {
        ICounterBuff counter = ItemCounter.Find(item, buffManager);
        return new ToolkitKitEntry
        {
            iconSource = item,
            title = item.title,
            body = ToolkitRichText.Normalise(ToolkitStatusText.Format(GetBody(item, style))),
            badge = counter != null ? ItemCounter.GetText(counter) : null,
            outline = style != null ? style.outlineColor : (Color?)null
        };
    }

    // The label of its style in its color (e.g. "Ascension"), then the description
    public static string GetBody(AItem item, ItemStyle style)
    {
        List<string> lines = new List<string>();
        if (style != null && !string.IsNullOrEmpty(style.label))
        {
            lines.Add($"<color=#{ColorUtility.ToHtmlStringRGB(style.outlineColor)}>{style.label}</color>");
        }

        if (!string.IsNullOrEmpty(item.description))
        {
            lines.Add(item.description);
        }

        return string.Join("\n", lines);
    }
}
