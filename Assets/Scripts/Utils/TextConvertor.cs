using System;
using System.Collections;
using System.Text.RegularExpressions;

public class TextConvertor
{
    static readonly string regexCatchVariable = @"{(.*?)}";
    static readonly string regexCatchExpression = @"\[(?>[^][]+|(?<c>)\[|(?<-c>)])+]";

    /// <summary>
    /// Convert variables within {} and expression within []
    /// </summary>
    public static string Convert(string description, Character character, object data)
    {
        if (string.IsNullOrEmpty(description))
        {
            return "";
        }

        // Replace all variables within {}
        description = Regex.Replace(description, regexCatchVariable, m => EvaluateVariable(character, data, m.Groups[1].Value, defaultValue: "1"));

        // Compute all expressions within []
        return Regex.Replace(description, regexCatchExpression, m => ExpressionEvaluator.Evaluate(m.Groups[0].Value.Replace(',', '.')).ToString());
    }

    /// <summary>
    /// Convert variables within {}
    /// attribute: base / current
    /// data: reflection within the object
    /// </summary>
    static string EvaluateVariable(Character character, object data, string variable, string defaultValue = "")
    {
        string[] tokens = variable.Split(":");
        if (tokens.Length == 0)
        {
            return "";
        }

        switch (tokens[0])
        {
            case "attribute":
                if (character == null)
                {
                    return defaultValue;
                }
                else if (tokens.Length < 3)
                {
                    return GetError("Wrong Format -> Available format is {attribute:param:type}", defaultValue);
                }

                if (Enum.TryParse<AttributeType>(tokens[2], out var type))
                {
                    Attribute attribute = character.attributeManager.Get(type);
                    return tokens[1] switch
                    {
                        "base" => attribute.BaseValue.ToString("F2"),
                        "current" => attribute.BaseValue.ToString("F2"),
                        _ => GetError($"Bad Attribute Param '{tokens[1]}' -> Available are 'base' or 'current'", defaultValue)
                    };
                }
                else
                {
                    return GetError("Wrong Type -> AttributeType", defaultValue);
                }

            case "data":
                if (data == null)
                {
                    return defaultValue;
                }
                else if (tokens.Length < 2)
                {
                    return GetError("Wrong Format -> Available format is {data:name}", defaultValue);
                }

                var value = GetPropertyValue(data, tokens[1]);
                return value != null ? value.ToString() : GetError($"Unkown Variable Name '{tokens[1]}'", defaultValue);
            default:
                return GetError($"Unkown Variable Type -> {tokens[0]}", defaultValue);
        }
    }

    public static object GetPropertyValue(object obj, string propertyName)
    {
        var propertyNames = propertyName.Split('.');

        for (var i = 0; i < propertyNames.Length; i++)
        {
            if (obj != null)
            {
                var tokens = propertyNames[i].Split('|');
                if (tokens.Length > 1 && int.TryParse(tokens[1], out int index))
                {
                    var listProp = obj.GetType().GetField(tokens[0]);
                    var list = listProp.GetValue(obj) as IList;
                    obj = list[index];
                }
                else
                {
                    var propertyInfo = obj.GetType().GetField(propertyNames[i]);
                    if (propertyInfo != null)
                    {
                        obj = propertyInfo.GetValue(obj);
                    }
                    else
                    {
                        obj = null;
                    }
                }
            }
        }

        return obj;
    }

    static string GetError(string errorLog, string output)
    {
        UnityEngine.Debug.LogError($"[EvaluateVariable] ERROR: {errorLog}");
        return output;
    }
}
