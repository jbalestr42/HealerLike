using System.Collections.Generic;

// Items tagged Cursed, a strong bonus and its curse on the same item, only come from events (e.g. the Dark
// Library): never as a regular reward, and they stand out among the items of the player
public static class CursedTag
{
    public const string Name = "Cursed";
    // Color of the curse texts
    public const string Color = "#B07CFF";

    public static bool IsCursed(AItem item)
    {
        return item != null && HasCursedTag(item.tags);
    }

    public static bool IsCursed(AItemFactory itemFactory)
    {
        return itemFactory != null && HasCursedTag(itemFactory.tags);
    }

    // The Cursed tag or one of its children (the depth limit only guards against a circular parent chain)
    static bool HasCursedTag(List<GameplayTag> tags)
    {
        if (tags == null)
        {
            return false;
        }

        foreach (GameplayTag tag in tags)
        {
            GameplayTag current = tag;
            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                if (current.name == Name)
                {
                    return true;
                }
                current = current.parent;
            }
        }
        return false;
    }
}
