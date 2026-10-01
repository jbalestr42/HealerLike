using System.Collections.Generic;

// Texts of a character card on the character select screen, kept apart from the UI so they can be tested
public static class CharacterCardText
{
    public static string GetDescription(CharacterData character)
    {
        return string.IsNullOrEmpty(character.text) ? "" : character.text;
    }

    // "<b>Shield</b>" then the description of the skill with its values computed from the stats of the
    // character (e.g. a heal from its Heal Power, starting items included), its cost and its cooldown
    public static string GetSkillTooltip(ACharacterSkillFactory skill, Character character)
    {
        CharacterSkillData data = skill.Create().GetData();
        string description = TextConvertor.Convert(data.description, character, data);
        return string.IsNullOrEmpty(description) ? $"<b>{data.name}</b>" : $"<b>{data.name}</b>\n{description}";
    }

    // The stats of the character, starting items included, as a list ("- Heal Power: 35")
    public static string GetStats(Character character)
    {
        List<string> lines = new List<string>();
        foreach (AttributeType type in System.Enum.GetValues(typeof(AttributeType)))
        {
            if (character.attributeManager.Has(type))
            {
                AddStat(lines, type, character.attributeManager.Get(type).Value);
            }
        }
        return string.Join("\n", lines);
    }

    // "- Max HP: 100", nothing for a stat at its default value: it says nothing (e.g. the damage of a unit
    // that doesn't attack)
    static void AddStat(List<string> lines, AttributeType type, float value)
    {
        if (!UnityEngine.Mathf.Approximately(value, AttributeManager.GetDefaultValue(type)))
        {
            lines.Add($"- {EntityInfoFormatter.GetAttributeName(type)}: {EntityInfoFormatter.FormatNumber(value)}");
        }
    }

    // The items the character starts with, one per line with its effect
    public static string GetItems(CharacterData character)
    {
        List<string> lines = new List<string>();
        if (character.items != null)
        {
            foreach (AItemFactory itemFactory in character.items)
            {
                if (itemFactory != null)
                {
                    string description = itemFactory.GetItem().description;
                    lines.Add("- " + itemFactory.title + (string.IsNullOrEmpty(description) ? "" : ": " + description));
                }
            }
        }
        return string.Join("\n", lines);
    }

    // Sections apart from each other: "<b>Zealot</b>" and its description, its stats as a list ("- Max HP: 100"),
    // then its innate items under "Passives"
    public static string GetUnitTooltip(EntityData entity)
    {
        List<string> sections = new List<string>();
        sections.Add(string.IsNullOrEmpty(entity.description) ? $"<b>{entity.title}</b>" : $"<b>{entity.title}</b>\n{entity.description}");

        if (entity.attributes != null)
        {
            List<AttributeType> types = new List<AttributeType>(entity.attributes.Keys);
            types.Sort();
            List<string> stats = new List<string>();
            foreach (AttributeType type in types)
            {
                AddStat(stats, type, entity.attributes[type]);
            }
            if (stats.Count > 0)
            {
                sections.Add(string.Join("\n", stats));
            }
        }

        List<string> passives = new List<string>();
        if (entity.items != null)
        {
            foreach (AItemFactory itemFactory in entity.items)
            {
                if (itemFactory != null)
                {
                    string description = itemFactory.GetItem().description;
                    passives.Add($"<b>{itemFactory.title}</b>" + (string.IsNullOrEmpty(description) ? "" : ": " + description));
                }
            }
        }
        if (passives.Count > 0)
        {
            sections.Add("Passives\n" + string.Join("\n", passives));
        }
        return string.Join("\n\n", sections);
    }
}
