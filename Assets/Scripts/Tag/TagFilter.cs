using System.Collections.Generic;

// Tag checks shared by everything taggable: whether it has a tag, and whether it matches a filter (every
// included tag and none of the excluded ones, a tag also matching its descendants)
public static class TagFilter
{
    // The depth limit only guards against a circular parent chain, real hierarchies are far shallower
    const int MaxDepth = 10;

    public static bool Matches(ITaggable taggable, ICollection<GameplayTag> includedTags, ICollection<GameplayTag> excludedTags)
    {
        if (taggable == null)
        {
            return false;
        }

        if (includedTags != null)
        {
            foreach (GameplayTag tag in includedTags)
            {
                if (!taggable.HasTag(tag))
                {
                    return false;
                }
            }
        }

        if (excludedTags != null)
        {
            foreach (GameplayTag tag in excludedTags)
            {
                if (taggable.HasTag(tag))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // One of the tags is the tag or one of its descendants
    public static bool HasTag(IEnumerable<GameplayTag> tags, GameplayTag tag)
    {
        if (tag == null || tags == null)
        {
            return false;
        }

        foreach (GameplayTag ownTag in tags)
        {
            if (ownTag != null && (ownTag == tag || ownTag.IsDescendantOf(tag)))
            {
                return true;
            }
        }
        return false;
    }

    // One of the tags, or one of their parents, has the name
    public static bool HasTag(IEnumerable<GameplayTag> tags, string tagName)
    {
        if (string.IsNullOrEmpty(tagName) || tags == null)
        {
            return false;
        }

        foreach (GameplayTag ownTag in tags)
        {
            GameplayTag current = ownTag;
            for (int depth = 0; depth < MaxDepth && current != null; depth++)
            {
                if (current.name == tagName)
                {
                    return true;
                }
                current = current.parent;
            }
        }
        return false;
    }
}
