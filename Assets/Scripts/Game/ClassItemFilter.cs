using System.Collections.Generic;

// Items made for a class (e.g. Holy Water, only useful with the Blessed status of the Cleric) carry its class tag and
// are only offered to the characters of that class; items without any class tag are offered to everyone
public static class ClassItemFilter
{
    // characters: every character of the game, whose class tags are the ones looked for on the item
    public static bool IsForCharacter(ITaggable item, CharacterData character, IEnumerable<CharacterData> characters)
    {
        bool hasClassTag = false;
        foreach (CharacterData other in characters)
        {
            if (other == null || other.classTag == null || !item.HasTag(other.classTag))
            {
                continue;
            }
            if (character != null && other.classTag == character.classTag)
            {
                return true;
            }
            hasClassTag = true;
        }
        return !hasClassTag;
    }
}
