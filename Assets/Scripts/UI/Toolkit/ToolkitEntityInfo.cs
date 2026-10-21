using System.Collections.Generic;
using UnityEngine;

// A detail section of the inspected creature: its rich text lines, and how many entries they hold
// (sub headings are lines, not entries)
public class ToolkitInfoSection
{
    public readonly List<string> lines = new List<string>();
    public int count = 0;
}

// What the detail panel shows about a creature beyond its summary. Every string comes from the
// sandbox formatters (EntityInfoFormatter, EntityInfoPanel) so their edits reach this panel.
// Only the lines their panel builds in private methods are assembled again here, from the same
// public formatter pieces: GetHealthLine, GetTargeting, GetSkills and GetEffects (on hit part).
// Delete those the day the sandbox panel makes its own versions public.
public static class ToolkitEntityInfo
{
    static readonly List<ASkill> SkillBuffer = new List<ASkill>();

    // "Health: 63 / 100 (63 %)", followed by " · Invulnerable" while the health can not be consumed
    public static string GetHealthLine(Entity entity)
    {
        ResourceAttribute health = entity.health;
        if (health == null)
        {
            return "";
        }

        float percent = ToolkitPresentation.Percentage(health.Value, health.Max);
        string line = $"Health: {ToolkitPresentation.Resource(health.Value, health.Max)} ({Mathf.RoundToInt(percent)} %)";
        if (health.preventConsumers)
        {
            line += " · Invulnerable";
        }

        return line;
    }

    // The creature description, then every attribute with the modifiers applied on it
    public static List<string> GetAttributeLines(Entity entity)
    {
        List<string> lines = new List<string>();
        if (entity.data != null && !string.IsNullOrEmpty(entity.data.description))
        {
            lines.Add($"<i>{entity.data.description}</i>");
        }

        if (entity.attributeManager != null)
        {
            lines.AddRange(EntityInfoFormatter.GetAttributeLines(entity.attributeManager, entity.gameObject));
        }

        return lines;
    }

    // Target count, the conditions a target must meet, and who the creature aims at right now
    public static ToolkitInfoSection GetTargeting(Entity entity)
    {
        ToolkitInfoSection section = new ToolkitInfoSection();
        TargetProvider targetProvider = entity.targetProvider;
        if (targetProvider == null)
        {
            return section;
        }

        section.lines.Add($"{targetProvider.targetBehaviourType} <color={EntityInfoFormatter.MutedColor}>· {targetProvider.targetCount} target(s)</color>");
        if (entity.data != null && entity.data.targetValidators != null)
        {
            foreach (ATargetValidatorFactory validator in entity.data.targetValidators)
            {
                if (validator != null)
                {
                    section.lines.Add($"<color={EntityInfoFormatter.MutedColor}>    condition: {EntityInfoFormatter.Prettify(validator.GetType().Name, "Factory", "Validator")}</color>");
                }
            }
        }

        List<string> targets = new List<string>();
        List<GameObject> currentTargets = targetProvider.GetTargets();
        if (currentTargets != null)
        {
            foreach (GameObject target in currentTargets)
            {
                if (target != null)
                {
                    targets.Add(EntityInfoFormatter.GetSourceName(target, entity.gameObject));
                }
            }
        }

        if (targets.Count > 0)
        {
            section.lines.Add($"Aiming at: {string.Join(", ", targets)}");
        }

        section.count = section.lines.Count;
        return section;
    }

    // The skills of the creature, the ones an item gave it flagged "(item)"
    public static ToolkitInfoSection GetSkills(Entity entity)
    {
        ToolkitInfoSection section = new ToolkitInfoSection();
        entity.GetComponents(SkillBuffer);
        foreach (ASkill skill in SkillBuffer)
        {
            string line = EntityInfoFormatter.FormatSkill(skill);
            if (!entity.skills.Contains(skill))
            {
                line += $" <color={EntityInfoFormatter.MutedColor}>(item)</color>";
            }

            section.lines.Add(line);
        }

        section.count = section.lines.Count;
        SkillBuffer.Clear();
        return section;
    }

    // The items the creature is born with, then the ones the player put in its inventory
    public static ToolkitInfoSection GetItems(Entity entity)
    {
        ToolkitInfoSection section = new ToolkitInfoSection();
        AddGroup(section, "Born with", EntityInfoPanel.GetPassiveLines(entity));
        AddGroup(section, "Equipped", EntityInfoPanel.GetEquippedItemLines(entity));
        return section;
    }

    // The effects active on the creature, then the ones its hits apply (duplicates grouped)
    public static ToolkitInfoSection GetEffects(Entity entity)
    {
        ToolkitInfoSection section = new ToolkitInfoSection();
        if (entity.buffManager != null)
        {
            AddGroup(section, "Active", EntityInfoFormatter.GetBuffLines(entity.buffManager, entity.gameObject));
        }

        AddGroup(section, "On hit", GetOnHitLines(entity));
        return section;
    }

    // One text for a label: the lines joined, in markup the Toolkit labels accept
    public static string Join(List<string> lines)
    {
        return ToolkitRichText.Normalise(string.Join("\n", lines), ToolkitRichTextOptions.StripSizes);
    }

    static List<string> GetOnHitLines(Entity entity)
    {
        List<string> lines = new List<string>();
        foreach (ABuffHandlerFactory onHitEffect in entity.GetOnHitEffects())
        {
            if (onHitEffect != null)
            {
                lines.Add($"Applies: {EntityInfoFormatter.GetBuffName(onHitEffect)}");
            }
        }

        foreach (AConsumerFactory consumer in entity.GetOnHitConsumers())
        {
            if (consumer != null)
            {
                lines.Add($"Consumer: {EntityInfoFormatter.Prettify(consumer.GetType().Name, "Factory", "Consumer")}");
            }
        }

        foreach (ABuffHandlerFactory projectileBehaviour in entity.projectileBehaviours)
        {
            if (projectileBehaviour != null)
            {
                lines.Add($"Projectile: {EntityInfoFormatter.GetBuffName(projectileBehaviour)}");
            }
        }

        // An item in the first slots is equipped several times, so its effects are added several times
        return EntityInfoFormatter.GroupDuplicates(lines);
    }

    static void AddGroup(ToolkitInfoSection section, string heading, List<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        section.lines.Add($"<color={EntityInfoFormatter.MutedColor}>{heading}</color>");
        section.lines.AddRange(lines);
        section.count += lines.Count;
    }
}
