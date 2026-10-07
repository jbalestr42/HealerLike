using System.Collections.Generic;
using UnityEngine;

// What a class card shows beyond its text: the real stats of a built preview of the class, and one icon entry per
// skill and per unit with its popover text. Built from Julien's CharacterSelectPanel.CreatePreview and
// CharacterCardText, so the numbers are the ones his select screen shows.
public class ToolkitClassKit
{
    readonly List<ToolkitKitEntry> _skills = new List<ToolkitKitEntry>();
    readonly List<ToolkitKitEntry> _units = new List<ToolkitKitEntry>();
    string _stats = "";

    public string stats { get { return _stats; } }

    public IReadOnlyList<ToolkitKitEntry> skills { get { return _skills; } }

    public IReadOnlyList<ToolkitKitEntry> units { get { return _units; } }

    // The skills then the units, the order the card lays its chips out in
    public List<ToolkitKitEntry> Entries()
    {
        List<ToolkitKitEntry> entries = new List<ToolkitKitEntry>(_skills);
        entries.AddRange(_units);
        return entries;
    }

    // The preview is built, read and destroyed inside this call: nothing of it outlives the method
    public static ToolkitClassKit Build(CharacterData character)
    {
        ToolkitClassKit kit = new ToolkitClassKit();
        if (character == null)
        {
            return kit;
        }

        Character preview = CanPreview(character) ? CharacterSelectPanel.CreatePreview(character, null) : null;
        if (preview != null)
        {
            kit._stats = Rich(CharacterCardText.GetStats(preview));
        }

        kit.AddSkills(character, preview);
        kit.AddUnits(character);
        DisposePreview(preview);
        return kit;
    }

    // His CreatePreview reads the attribute table and every starting item without a null check
    static bool CanPreview(CharacterData character)
    {
        bool hasEmptyItem = false;
        if (character.items != null)
        {
            foreach (AItemFactory item in character.items)
            {
                hasEmptyItem = hasEmptyItem || item == null;
            }
        }

        if (character.attributes == null || character.items == null || hasEmptyItem)
        {
            Debug.LogError("[ToolkitClassKit] '" + character.title + "' has no attribute table or an empty starting item, no stats shown.");
            return false;
        }

        return true;
    }

    // Deactivated first, so a Destroy deferred to the end of a play frame leaves nothing ticking in the meantime
    static void DisposePreview(Character preview)
    {
        if (preview == null)
        {
            return;
        }

        GameObject owner = preview.gameObject;
        owner.SetActive(false);
        if (Application.isPlaying)
        {
            Object.Destroy(owner);
        }
        else
        {
            Object.DestroyImmediate(owner);
        }
    }

    void AddSkills(CharacterData character, Character preview)
    {
        if (character.skills == null)
        {
            return;
        }

        foreach (ACharacterSkillFactory skill in character.skills)
        {
            if (skill == null)
            {
                continue;
            }

            // The same data object the spell bar hands to the icon service, so a class card and a cast bar agree
            CharacterSkillData data = skill.Create().GetData();
            _skills.Add(new ToolkitKitEntry
            {
                iconSource = data,
                title = DataIconSource.Label(data),
                body = Rich(TooltipBody(CharacterCardText.GetSkillTooltip(skill, preview), data.name)),
                showLabel = false
            });
        }
    }

    void AddUnits(CharacterData character)
    {
        if (character.entities == null)
        {
            return;
        }

        foreach (EntityData entity in character.entities)
        {
            if (entity == null)
            {
                continue;
            }

            _units.Add(new ToolkitKitEntry
            {
                iconSource = entity,
                title = DataIconSource.Label(entity),
                body = Rich(TooltipBody(CharacterCardText.GetUnitTooltip(entity), entity.title)),
                showLabel = true
            });
        }
    }

    // His tooltips open with the name in bold, which the popover already shows as its title. Anything else is kept
    // whole, so a change of his format repeats the name instead of losing text.
    public static string TooltipBody(string tooltip, string name)
    {
        string head = "<b>" + name + "</b>";
        if (string.IsNullOrEmpty(tooltip))
        {
            return "";
        }

        if (!tooltip.StartsWith(head, System.StringComparison.Ordinal))
        {
            return tooltip;
        }

        return tooltip.Substring(head.Length).TrimStart('\n');
    }

    // DEPENDENCY: ToolkitRichText.Normalise, written by another patch (see SHARED.md). Its fallback is in the
    // patch's fallback/ folder: apply that file only if the other patch is not applied.
    static string Rich(string text)
    {
        return ToolkitRichText.Normalise(text);
    }
}
