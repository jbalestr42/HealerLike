using System.Collections.Generic;

// Texts of a character card on the character select screen, kept apart from the UI so they can be tested
public static class CharacterCardText
{
    public static string GetDescription(CharacterData character)
    {
        return string.IsNullOrEmpty(character.text) ? "" : character.text;
    }

    // One skill per line
    public static string GetSkills(CharacterData character)
    {
        List<string> names = new List<string>();
        if (character.skills != null)
        {
            foreach (ACharacterSkillFactory skill in character.skills)
            {
                if (skill != null)
                {
                    names.Add("- " + skill.Create().GetData().name);
                }
            }
        }
        return string.Join("\n", names);
    }

    // The units the character can recruit, on a single line
    public static string GetUnits(CharacterData character)
    {
        List<string> titles = new List<string>();
        if (character.entities != null)
        {
            foreach (EntityData entity in character.entities)
            {
                if (entity != null)
                {
                    titles.Add(entity.title);
                }
            }
        }
        return string.Join(", ", titles);
    }
}
