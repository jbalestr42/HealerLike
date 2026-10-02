using System.Collections.Generic;

// Whether an item matches a tag filter: every included tag and none of the excluded ones, a tag also matching
// its descendants (e.g. the player items that aren't cursed)
public static class ItemTagFilter
{
    public static bool Matches(AItemFactory item, ICollection<GameplayTag> includedTags, ICollection<GameplayTag> excludedTags)
    {
        if (item == null)
        {
            return false;
        }

        if (includedTags != null)
        {
            foreach (GameplayTag tag in includedTags)
            {
                if (!HasTag(item, tag))
                {
                    return false;
                }
            }
        }

        if (excludedTags != null)
        {
            foreach (GameplayTag tag in excludedTags)
            {
                if (HasTag(item, tag))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool HasTag(AItemFactory item, GameplayTag tag)
    {
        return tag != null && item.tags != null && item.tags.Exists(itemTag => itemTag != null && (itemTag == tag || itemTag.IsDescendantOf(tag)));
    }
}
