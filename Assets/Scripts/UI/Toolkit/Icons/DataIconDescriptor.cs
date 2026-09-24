using System;
using System.Reflection;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Icons
{
    public enum DataIconKind { Spell, Creature, Character, Data }

    public enum DataIconSymbol { Generic, Heal, Bolt, Shield, Poison, Flame, Slime, Dragon, Fox, Soldier, Swarm, Archer, Mage }

    /// <summary>Presentation metadata independent of UI layout and asset import APIs.</summary>
    public readonly struct DataIconDescriptor
    {
        public readonly string Key;
        public readonly string Label;
        public readonly DataIconKind Kind;
        public DataIconSymbol Symbol => ResolveSymbol(Label, Kind);

        public static DataIconSymbol ResolveSymbol(string text, DataIconKind kind)
        {
            string value = (text ?? string.Empty).ToLowerInvariant();
            if (kind == DataIconKind.Creature || kind == DataIconKind.Character)
            {
                if (value.Contains("slime")) return DataIconSymbol.Slime;
                if (value.Contains("dragon")) return DataIconSymbol.Dragon;
                if (value.Contains("fox") || value.Contains("wolf")) return DataIconSymbol.Fox;
                if (value.Contains("swarm")) return DataIconSymbol.Swarm;
                if (value.Contains("soldier") || value.Contains("armor")) return DataIconSymbol.Soldier;
                if (value.Contains("shoot") || value.Contains("shot")) return DataIconSymbol.Archer;
                if (value.Contains("channel") || value.Contains("lightning") || value.Contains("buff")) return DataIconSymbol.Mage;
            }
            if (value.Contains("heal") || value.Contains("restore")) return DataIconSymbol.Heal;
            if (value.Contains("poison") || value.Contains("venom")) return DataIconSymbol.Poison;
            if (value.Contains("fire") || value.Contains("flame") || value.Contains("burn")) return DataIconSymbol.Flame;
            if (value.Contains("buff") || value.Contains("shield") || value.Contains("armor") || value.Contains("reducedamage")) return DataIconSymbol.Shield;
            if (value.Contains("projectile") || value.Contains("damage") || value.Contains("lightning") || value.Contains("shoot")) return DataIconSymbol.Bolt;
            return DataIconSymbol.Generic;
        }
        public DataIconDescriptor(string key, string label, DataIconKind kind)
        {
            Key = key ?? string.Empty;
            Label = label ?? string.Empty;
            Kind = kind;
        }

        public static DataIconDescriptor From(object source)
        {
            if (source == null || source is UnityEngine.Object unityObject && !unityObject)
                return new DataIconDescriptor("missing", "Unknown", DataIconKind.Data);
            var nested = source is ACharacterSkill skill ? skill.GetData() : ReadField(source, "data");
            nested = nested ?? source;
            var kind = nested is EntityData ? DataIconKind.Creature :
                nested is CharacterData ? DataIconKind.Character :
                nested is CharacterSkillData || nested is SkillDataBase ||
                source is ACharacterSkillFactory || source is ASkillFactory
                    ? DataIconKind.Spell : KindFromNamespace(nested.GetType().Namespace);
            string label = ReadField(nested, "title") as string;
            if (string.IsNullOrWhiteSpace(label)) label = ReadField(nested, "name") as string;
            if (string.IsNullOrWhiteSpace(label) && nested is UnityEngine.Object asset) label = asset.name;
            if (string.IsNullOrWhiteSpace(label)) label = nested.GetType().Name;
            return new DataIconDescriptor(kind + ":" + nested.GetType().FullName + ":" + label, label, kind);
        }

        public static DataIconKind KindFromNamespace(string dataNamespace)
        {
            // Namespace convention avoids a circular Runtime -> Render assembly reference.
            if (dataNamespace == "HealerLike.Render.Creatures" ||
                (dataNamespace ?? string.Empty).StartsWith("HealerLike.Render.Creatures.", StringComparison.Ordinal))
                return DataIconKind.Creature;
            if (dataNamespace == "HealerLike.Render.Spells" ||
                (dataNamespace ?? string.Empty).StartsWith("HealerLike.Render.Spells.", StringComparison.Ordinal))
                return DataIconKind.Spell;
            return DataIconKind.Data;
        }

        internal static object ReadField(object source, string name)
        {
            return source?.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(source);
        }

        // String.GetHashCode is intentionally avoided: its result can change between processes.
        public static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty) hash = (hash ^ character) * 16777619;
                return hash;
            }
        }
    }
}
