using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// A dialog of cards as wide as its cards: all of them on one row when the screen allows, else as many as fit a row, so
// 2, 3 or n choices sit centered in a dialog fitted to them. Its card lists are measured each time they are laid out
// again (new cards, a resize). A phone keeps its full-width dialogs.
public static class ToolkitDialogFit
{
    public static void Register(VisualElement root)
    {
        foreach (VisualElement dialog in root.Query(className: "dialog").ToList())
        {
            List<ScrollView> lists = dialog.Query<ScrollView>(className: "collection-list").ToList();
            foreach (ScrollView list in lists)
            {
                list.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Fit(root, dialog, lists));
            }
        }
    }

    static void Fit(VisualElement root, VisualElement dialog, List<ScrollView> lists)
    {
        if (root.ClassListContains(ToolkitResponsiveLayout.PhoneClass) || dialog.parent == null)
        {
            dialog.style.width = StyleKeyword.Null;
            return;
        }

        float contentWidth = 0f;
        foreach (ScrollView list in lists)
        {
            contentWidth = Mathf.Max(contentWidth, list.contentContainer.layout.width);
        }

        // The padding, borders and scroll bar around the cards, kept as they are
        float chrome = dialog.layout.width - contentWidth;
        float available = dialog.parent.layout.width - dialog.parent.resolvedStyle.paddingLeft
            - dialog.parent.resolvedStyle.paddingRight;
        if (dialog.resolvedStyle.maxWidth.keyword == StyleKeyword.Undefined && dialog.resolvedStyle.maxWidth.value > 0f)
        {
            available = Mathf.Min(available, dialog.resolvedStyle.maxWidth.value);
        }

        float needed = 0f;
        foreach (ScrollView list in lists)
        {
            needed = Mathf.Max(needed, RowWidth(list.contentContainer, available - chrome));
        }

        if (needed <= 0f || float.IsNaN(chrome) || float.IsNaN(available))
        {
            return;
        }

        // Rounded up, a width a fraction short would wrap the last card
        dialog.style.width = Mathf.Ceil(chrome + needed) + 1f;
    }

    // The width of the cards' widest row: all of them, or as many as the room allows in balanced rows
    static float RowWidth(VisualElement content, float room)
    {
        if (content.childCount == 0)
        {
            return 0f;
        }

        VisualElement card = content[0];
        float cardWidth = card.layout.width + card.resolvedStyle.marginLeft + card.resolvedStyle.marginRight;
        if (float.IsNaN(cardWidth) || cardWidth <= 0f)
        {
            return 0f;
        }

        // The rows balanced, 6 cards that fit 5 a row go 3 and 3 rather than 5 and 1
        int fit = Mathf.Max(1, Mathf.FloorToInt(room / cardWidth));
        int rows = Mathf.CeilToInt(content.childCount / (float)fit);
        int perRow = Mathf.CeilToInt(content.childCount / (float)rows);
        return perRow * cardWidth + content.resolvedStyle.paddingLeft + content.resolvedStyle.paddingRight;
    }
}
