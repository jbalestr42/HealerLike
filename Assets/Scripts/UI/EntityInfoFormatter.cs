using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

// Rich texts of the entity info panel, kept apart from the UI so they can be tested
public static class EntityInfoFormatter
{
    public const string HeaderColor = "#FFD966";
    public const string MutedColor = "#9AA3B2";
    public const string BonusColor = "#7CD67C";
    public const string MalusColor = "#E86A6A";

    static readonly Dictionary<AttributeType, string> AttributeNames = new Dictionary<AttributeType, string>
    {
        { AttributeType.HealthMax, "Max HP" },
        { AttributeType.AttackRate, "Attack Cooldown" },
        { AttributeType.Damage, "Damage" },
        { AttributeType.Range, "Range" },
        { AttributeType.FlatArmor, "Armor" },
        { AttributeType.PercentArmor, "Armor %" },
        { AttributeType.HitArmor, "Hit Armor" },
        { AttributeType.Speed, "Speed" },
        { AttributeType.Vulnerability, "Vulnerability" },
        { AttributeType.ManaMax, "Max Mana" },
        { AttributeType.HealPower, "Heal Power" },
        { AttributeType.CriticalChance, "Critical Chance" },
        { AttributeType.CriticalMultiplier, "Critical Multiplier" },
        { AttributeType.CriticalChanceResist, "Critical Resist" },
        { AttributeType.HealingReceived, "Healing Received" },
        { AttributeType.SkillCooldownMultiplier, "Skill Cooldown Multiplier" },
    };

    static readonly AttributeModifierType[] ModifierTypes = { AttributeModifierType.Add, AttributeModifierType.Multiply, AttributeModifierType.Override };

    public static string GetAttributeName(AttributeType type)
    {
        return AttributeNames.TryGetValue(type, out string name) ? name : type.ToString();
    }

    public static string FormatNumber(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string FormatDuration(float seconds)
    {
        return seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
    }

    // "ShootProjectileSkillFactory" -> "Shoot Projectile"
    public static string Prettify(string typeName, params string[] suffixes)
    {
        string name = typeName;
        int genericIndex = name.IndexOf('`');
        if (genericIndex >= 0)
        {
            name = name.Substring(0, genericIndex);
        }

        bool removed = true;
        while (removed)
        {
            removed = false;
            foreach (string suffix in suffixes)
            {
                if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    removed = true;
                }
            }
        }

        name = name.Replace('_', ' ');
        name = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
        return Regex.Replace(name, " +", " ").Trim();
    }

    // Identical lines are merged into one, suffixed by their count: "Poison ×3"
    public static List<string> GroupDuplicates(List<string> lines)
    {
        List<string> distinct = new List<string>();
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string line in lines)
        {
            if (counts.ContainsKey(line))
            {
                counts[line]++;
            }
            else
            {
                counts[line] = 1;
                distinct.Add(line);
            }
        }
        return distinct.ConvertAll(line => counts[line] > 1 ? $"{line} ×{counts[line]}" : line);
    }

    // The entity title when the source is an entity, its name otherwise
    public static string GetSourceName(GameObject source, GameObject owner)
    {
        if (source == null)
        {
            return "missing source";
        }
        if (source == owner)
        {
            return "itself";
        }

        Entity entity = source.GetComponent<Entity>();
        if (entity != null && entity.data != null)
        {
            return entity.data.title;
        }
        return source.name;
    }

    #region Attributes

    // "Damage: 12 (base 10)", the base is only shown when modifiers change the value
    public static string FormatAttribute(AttributeType type, Attribute attribute)
    {
        string line = $"{GetAttributeName(type)}: <b>{FormatNumber(attribute.Value)}</b>";
        if (!Mathf.Approximately(attribute.Value, attribute.BaseValue))
        {
            string color = attribute.Value > attribute.BaseValue ? BonusColor : MalusColor;
            line += $" <color={color}>(base {FormatNumber(attribute.BaseValue)})</color>";
        }
        return line;
    }

    // "+2 · Hexer", "+30 % · itself", "= 5 · Frost Shooter"
    public static string FormatModifier(AttributeModifierType type, Attribute.SourceModifier sourceModifier, GameObject owner)
    {
        float value = sourceModifier.modifier.ApplyModifier();
        string sign = value >= 0f ? "+" : "";
        string amount;
        switch (type)
        {
            case AttributeModifierType.Add:
                amount = sign + FormatNumber(value);
                break;
            case AttributeModifierType.Multiply:
                amount = sign + FormatNumber(value * 100f) + " %";
                break;
            default:
                amount = "= " + FormatNumber(value);
                break;
        }
        return $"<color={MutedColor}>    {amount} · {GetSourceName(sourceModifier.source, owner)}</color>";
    }

    // Every attribute of the entity, each followed by the modifiers applied on it
    public static List<string> GetAttributeLines(AttributeManager attributeManager, GameObject owner)
    {
        List<string> lines = new List<string>();
        foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
        {
            if (!attributeManager.Has(type))
            {
                continue;
            }

            Attribute attribute = attributeManager.Get(type);
            lines.Add(FormatAttribute(type, attribute));
            foreach (AttributeModifierType modifierType in ModifierTypes)
            {
                foreach (Attribute.SourceModifier sourceModifier in attribute.GetModifiers(modifierType))
                {
                    lines.Add(FormatModifier(modifierType, sourceModifier, owner));
                }
            }
        }
        return lines;
    }

    #endregion

    #region Buffs

    // "Vulnerability (Flat Modifier)" for an attribute modifier, the buff type otherwise
    public static string DescribeBuff(ABuffFactory buffFactory)
    {
        string typeName = Prettify(buffFactory.GetType().Name, "Factory", "Buff");
        if (buffFactory.buffData is BaseData attributeData)
        {
            return $"{GetAttributeName(attributeData.type)} ({typeName})";
        }
        return typeName;
    }

    // The asset name when it means something, what the buffs do otherwise (most assets are
    // just called "BuffHandlerFactory")
    public static string GetBuffName(ABuffHandlerFactory buffHandlerFactory)
    {
        string name = buffHandlerFactory.name.Replace(" ", "");
        int genericIndex = name.IndexOf("BuffHandlerFactory", StringComparison.OrdinalIgnoreCase);
        if (genericIndex >= 0)
        {
            name = name.Substring(0, genericIndex).TrimEnd('_');
        }
        if (name.Length > 0 && name != "New")
        {
            return Prettify(name);
        }

        List<string> buffs = new List<string>();
        foreach (ABuffFactory buffFactory in buffHandlerFactory.buffFactoryList)
        {
            if (buffFactory != null)
            {
                buffs.Add(DescribeBuff(buffFactory));
            }
        }
        return buffs.Count > 0 ? string.Join(", ", buffs) : "Effect";
    }

    // "Vulnerability (Flat Modifier) ×2 · 3.2s · from Hexer"
    public static string FormatBuff(BuffManager.BuffHandlerData buffHandlerData, GameObject owner)
    {
        string line = $"<b>{GetBuffName(buffHandlerData.buffHandlerFactory)}</b>";
        if (buffHandlerData.currentStacks > 1)
        {
            line += $" ×{buffHandlerData.currentStacks}";
        }

        ABuffHandler buffHandler = buffHandlerData.buffHandler;
        string duration = buffHandler.durationType == DurationType.Duration ? FormatDuration(buffHandler.remainingDuration) : "permanent";
        line += $" <color={MutedColor}>· {duration} · from {GetSourceName(buffHandlerData.source, owner)}</color>";

        List<string> tags = new List<string>();
        foreach (GameplayTag tag in buffHandlerData.buffHandlerFactory.tags)
        {
            if (tag != null)
            {
                tags.Add(tag.name);
            }
        }
        if (tags.Count > 0)
        {
            line += $"\n<color={MutedColor}>    tags: {string.Join(", ", tags)}</color>";
        }
        return line;
    }

    public static List<string> GetBuffLines(BuffManager buffManager, GameObject owner)
    {
        List<string> lines = new List<string>();
        foreach (BuffManager.BuffHandlerData buffHandlerData in buffManager.GetActiveHandlers())
        {
            lines.Add(FormatBuff(buffHandlerData, owner));
        }
        return lines;
    }

    #endregion

    #region Skills and items

    // "Shoot Projectile · cooldown 1.5s · ready"
    public static string FormatSkill(ASkill skill)
    {
        string line = $"<b>{Prettify(skill.GetType().Name, "Skill")}</b>";
        if (skill is ICooldownSkill cooldownSkill)
        {
            float remaining = Mathf.Max(0f, cooldownSkill.cooldownProgress * cooldownSkill.cooldownDuration);
            string state = remaining > 0f ? $"ready in {FormatDuration(remaining)}" : "ready";
            line += $" <color={MutedColor}>· cooldown {FormatDuration(cooldownSkill.cooldownDuration)} · {state}</color>";
        }
        return line;
    }

    // "Venom — Poisons the target (innate)"
    public static string FormatItem(AItem item, bool isInnate)
    {
        string line = $"<b>{item.title}</b>";
        if (!string.IsNullOrEmpty(item.description))
        {
            line += $" — {item.description}";
        }
        line += $" <color={MutedColor}>({(isInnate ? "innate" : "added")})</color>";
        return line;
    }

    #endregion
}
