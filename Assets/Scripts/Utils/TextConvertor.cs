using System;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

public class TextConvertor
{
    public static string GetDescription(Character character, object data, string description)
    {
        var output = Regex.Replace(description, @"{(.*?)}", m => ResolveValue(character, data, m.Groups[1].Value));
        return output;
    }

    public static string ResolveValue(Character character, object data, string value)
    {
        string[] operators = value.Split("|");
        if (operators.Length == 0)
        {
            return "null";
        }

        string[] tokens = operators[0].Split(":");
        if (tokens.Length == 0)
        {
            return "null";
        }

        switch (tokens[0])
        {
            case "attribute":
                if (tokens.Length < 3)
                {
                    return GetError("Wrong Format -> {attribute:param:type}");
                }

                if (Enum.TryParse<AttributeType>(tokens[2], out var type))
                {
                    Attribute attribute = character.attributeManager.Get(type);
                    return tokens[1] switch
                    {
                        "base" => GetAttribute(attribute.BaseValue),
                        "current" => GetAttribute(attribute.Value),
                        // TODO how to get the value if the skill also modifies it?
                        _ => GetError($"Bad Token '{tokens[1]}' -> base/current")
                    };
                }
                else
                {
                    return GetError("Wrong Type -> AttributeType");
                }

            case "data":
                if (operators.Length < 1)
                {
                    return GetError("Wrong Format -> {data:value}");
                }

                // TODO manage other types
                if (float.TryParse(data.GetType().GetField(tokens[1]).GetValue(data).ToString(), out var finalValue))
                {
                    if (operators.Length > 1 && float.TryParse(operators[1].Substring(4, operators[1].Length - 5), out var modifier))
                    {
                        return operators[1].Substring(0, 3) switch
                        {
                            "add" => GetData(finalValue + modifier),
                            "sub" => GetData(finalValue - modifier),
                            "mul" => GetData(finalValue * modifier),
                            "div" => GetData(finalValue + modifier),
                            _ => GetError($"Bad Token '{tokens[1]}' -> base/current")
                        };
                    }
                    else
                    {
                        return GetData(finalValue);
                    }
                }
                return GetError("Bad Type -> float");
            
            default:
                return "null";
        }
    }

    public static string GetAttribute(float value)
    {
        return $"<color=\"blue\">{value:F0}</color>";
    }

    public static string GetData(float value)
    {
        return $"<color=#008080ff>{value:F0}</color>";
    }

    public static string GetError(string error)
    {
        return $"<color=\"red\">[ERROR: {error}]</color>";
    }
}
