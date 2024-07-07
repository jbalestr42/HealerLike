using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector.Editor;

public class TextConvertor
{
    static readonly char openSign = '[';
    static readonly char closeSign = ']';
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
        return Regex.Replace(description, regexCatchExpression, m => EvaluateExpression(m.Groups[0].Value.Replace(',', '.')).ToString());
    }

    /// <summary>
    /// Resolve expression of type [[AxB]+C]
    /// </summary>
    /// <returns>Returns evaluated expression as float</returns>
    public static float EvaluateExpression(string expression)
    {
        Stack<float> values = new Stack<float>();
        Stack<char> operators = new Stack<char>();

        int i = 0;
        while (i < expression.Length)
        {
            int j = 0;
            while (i + j < expression.Length && (char.IsNumber(expression[i + j]) || expression[i + j] == '.' || expression[i + j] == ','))
            {
                j++;
            }

            if (j > 0)
            {
                values.Push(float.Parse(expression.Substring(i, j), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
                i += j;
            }

            if (i >= expression.Length)
            {
                break;
            }

            if (expression[i] == openSign)
            {
                operators.Push(openSign);
            }
            else if (expression[i] == closeSign)
            {
                while (operators.Peek() != openSign)
                {
                    values.Push(ComputeResult(values.Pop(), values.Pop(), operators.Pop()));
                }
                operators.Pop();
            }
            else if (IsOperator(expression[i]))
            {
                while (operators.Count != 0 && HasPrecedence(expression[i], operators.Peek()))
                {
                    values.Push(ComputeResult(values.Pop(), values.Pop(), operators.Pop()));
                }
                operators.Push(expression[i]);
            }
            i++;
        }

        while (operators.Count != 0)
        {
            values.Push(ComputeResult(values.Pop(), values.Pop(), operators.Pop()));
        }
        return values.Pop();
    }

    /// <summary>
    /// Returns true if 'op2' has higher or same precedence as 'op1',
    /// </summary>
    static bool HasPrecedence(char op1, char op2)
    {
        if (op2 == openSign || op2 == closeSign)
        {
            return false;
        }

        if ((op1 == 'x' || op1 == '/') && (op2 == '+' || op2 == '-'))
        {
            return false;
        }

        return true;
    }

    static bool IsOperator(char c)
    {
        return c == '+' || c == '-' || c == 'x' || c == '/';
    }

    static float ComputeResult(float a, float b, char op)
    {
        return op switch
        {
            '+' => b + a,
            '-' => b - a,
            'x' => b * a,
            '/' => b / a,
            _ => 1f
        };
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

                if (tokens.Length > 3)
                {
                    if (int.TryParse(tokens[2], out int index))
                    {
                        var listProp = data.GetType().GetField(tokens[1]);
                        var list = listProp.GetValue(data) as IList;
                        var value = GetPropertyValue(list[index], tokens[3]);
                        return value != null ? value.ToString() : GetError($"Unkown Variable Name '{tokens[1]}'", defaultValue);
                    }
                    else
                    {
                        return GetError("", defaultValue);
                    }
                }
                else
                {
                    var value = data.GetType().GetField(tokens[1])?.GetValue(data);
                    return value != null ? value.ToString() : GetError($"Unkown Variable Name '{tokens[1]}'", defaultValue);
                }

            default:
                return GetError($"Unkown Variable Type -> {tokens[0]}", defaultValue);
        }
    }

    public static object GetPropertyValue(object obj, string propertyName)
    {
        var _propertyNames = propertyName.Split('.');

        for (var i = 0; i < _propertyNames.Length; i++)
        {
            if (obj != null)
            {
                var _propertyInfo = obj.GetType().GetField(_propertyNames[i]);
                if (_propertyInfo != null)
                {
                    obj = _propertyInfo.GetValue(obj);
                }
                else
                {
                    obj = null;
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
