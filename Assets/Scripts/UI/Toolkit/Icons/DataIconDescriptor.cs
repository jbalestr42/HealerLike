using System;
using System.Reflection;
using UnityEngine;

public enum DataIconKind
{
    Spell,
    Creature,
    Character,
    Data
}

public enum DataIconSymbol
{
    Generic,
    Heal,
    Bolt,
    Shield,
    Poison,
    Flame,
    Slime,
    Dragon,
    Fox,
    Soldier,
    Swarm,
    Archer,
    Mage
}

// Presentation metadata of a data object, independent of the UI layout and of the asset import
public class DataIconDescriptor
{
    static readonly string renderCreaturesNamespace = "HealerLike.Render.Creatures";
    static readonly string renderSpellsNamespace = "HealerLike.Render.Spells";

    string _key;
    public string key { get { return _key; } }

    string _label;
    public string label { get { return _label; } }

    DataIconKind _kind;
    public DataIconKind kind { get { return _kind; } }

    public DataIconSymbol symbol { get { return ResolveSymbol(_label, _kind); } }

    public DataIconDescriptor(string key, string label, DataIconKind kind)
    {
        _key = key != null ? key : string.Empty;
        _label = label != null ? label : string.Empty;
        _kind = kind;
    }

    public static DataIconDescriptor From(object source)
    {
        if (source == null || (source is UnityEngine.Object && !(UnityEngine.Object)source))
        {
            return new DataIconDescriptor("missing", "Unknown", DataIconKind.Data);
        }

        object nested;
        if (source is ACharacterSkill)
        {
            nested = ((ACharacterSkill)source).GetData();
        }
        else
        {
            nested = ReadField(source, "data");
        }

        if (nested == null)
        {
            nested = source;
        }

        DataIconKind kind = GetKind(source, nested);
        string label = GetLabel(nested);
        return new DataIconDescriptor(kind + ":" + nested.GetType().FullName + ":" + label, label, kind);
    }

    static DataIconKind GetKind(object source, object nested)
    {
        if (nested is EntityData)
        {
            return DataIconKind.Creature;
        }

        if (nested is CharacterData)
        {
            return DataIconKind.Character;
        }

        if (nested is CharacterSkillData || nested is SkillDataBase
            || source is ACharacterSkillFactory || source is ASkillFactory)
        {
            return DataIconKind.Spell;
        }

        return KindFromNamespace(nested.GetType().Namespace);
    }

    static string GetLabel(object nested)
    {
        string label = ReadField(nested, "title") as string;
        if (string.IsNullOrWhiteSpace(label))
        {
            label = ReadField(nested, "name") as string;
        }

        if (string.IsNullOrWhiteSpace(label) && nested is UnityEngine.Object)
        {
            label = ((UnityEngine.Object)nested).name;
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            label = nested.GetType().Name;
        }

        return label;
    }

    // The namespace convention avoids a circular Runtime to Render assembly reference
    public static DataIconKind KindFromNamespace(string dataNamespace)
    {
        string value = dataNamespace != null ? dataNamespace : string.Empty;
        if (value == renderCreaturesNamespace
            || value.StartsWith(renderCreaturesNamespace + ".", StringComparison.Ordinal))
        {
            return DataIconKind.Creature;
        }

        if (value == renderSpellsNamespace
            || value.StartsWith(renderSpellsNamespace + ".", StringComparison.Ordinal))
        {
            return DataIconKind.Spell;
        }

        return DataIconKind.Data;
    }

    public static object ReadField(object source, string name)
    {
        if (source == null)
        {
            return null;
        }

        FieldInfo field = source.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            return null;
        }

        return field.GetValue(source);
    }

    // String.GetHashCode is avoided on purpose: its result can change between processes
    public static uint StableHash(string value)
    {
        uint hash = 2166136261;
        if (value == null)
        {
            return hash;
        }

        foreach (char character in value)
        {
            hash = (hash ^ character) * 16777619;
        }

        return hash;
    }

    public static DataIconSymbol ResolveSymbol(string text, DataIconKind kind)
    {
        string value = text != null ? text.ToLowerInvariant() : string.Empty;
        if (kind == DataIconKind.Creature || kind == DataIconKind.Character)
        {
            DataIconSymbol creatureSymbol = ResolveCreatureSymbol(value);
            if (creatureSymbol != DataIconSymbol.Generic)
            {
                return creatureSymbol;
            }
        }

        if (value.Contains("heal") || value.Contains("restore"))
        {
            return DataIconSymbol.Heal;
        }

        if (value.Contains("poison") || value.Contains("venom"))
        {
            return DataIconSymbol.Poison;
        }

        if (value.Contains("fire") || value.Contains("flame") || value.Contains("burn"))
        {
            return DataIconSymbol.Flame;
        }

        if (value.Contains("buff") || value.Contains("shield") || value.Contains("armor")
            || value.Contains("reducedamage"))
        {
            return DataIconSymbol.Shield;
        }

        if (value.Contains("projectile") || value.Contains("damage") || value.Contains("lightning")
            || value.Contains("shoot"))
        {
            return DataIconSymbol.Bolt;
        }

        return DataIconSymbol.Generic;
    }

    static DataIconSymbol ResolveCreatureSymbol(string value)
    {
        if (value.Contains("slime"))
        {
            return DataIconSymbol.Slime;
        }

        if (value.Contains("dragon"))
        {
            return DataIconSymbol.Dragon;
        }

        if (value.Contains("fox") || value.Contains("wolf"))
        {
            return DataIconSymbol.Fox;
        }

        if (value.Contains("swarm"))
        {
            return DataIconSymbol.Swarm;
        }

        if (value.Contains("soldier") || value.Contains("armor"))
        {
            return DataIconSymbol.Soldier;
        }

        if (value.Contains("shoot") || value.Contains("shot"))
        {
            return DataIconSymbol.Archer;
        }

        if (value.Contains("channel") || value.Contains("lightning") || value.Contains("buff"))
        {
            return DataIconSymbol.Mage;
        }

        return DataIconSymbol.Generic;
    }
}
