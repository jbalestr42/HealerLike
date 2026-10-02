// Anything carrying gameplay tags (items, units, buffs), so they're all checked and filtered the same way
public interface ITaggable
{
    // Has the tag, or one of its descendants
    bool HasTag(GameplayTag tag);

    // Same by name: one of its tags, or a parent of it, has this name (e.g. TagNames.Cursed)
    bool HasTag(string tagName);
}
