using System.Collections.Generic;
using UnityEngine;

// The styles of the items (e.g. cursed, ascension), each one for a tag; referenced by the GameData
[CreateAssetMenu(menuName = "Custom/Data/Item Style Set")]
public class ItemStyleSet : ScriptableObject
{
    public List<ItemStyle> styles = new List<ItemStyle>();

    // The style of the item's tags or of their parents, the highest priority first; null for a regular item
    public ItemStyle GetStyle(AItem item)
    {
        return item == null ? null : GetStyle(item.tags);
    }

    public ItemStyle GetStyle(IEnumerable<GameplayTag> tags)
    {
        if (tags == null)
        {
            return null;
        }

        ItemStyle best = null;
        foreach (GameplayTag tag in tags)
        {
            if (tag == null)
            {
                continue;
            }
            foreach (ItemStyle style in styles)
            {
                if (style != null && style.tag != null && (style.tag == tag || tag.IsDescendantOf(style.tag))
                    && (best == null || style.priority > best.priority))
                {
                    best = style;
                }
            }
        }
        return best;
    }
}
