using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Assertions;

public class TextConvertor
{
    static readonly char openSign = '[';
    static readonly char closeSign = ']';
    static readonly string regexCatchVariable = @"{(.*?)}";
    static readonly string regexCatchExpression = @"\[(?>[^][]+|(?<c>)\[|(?<-c>)])+]";
    static readonly char unaryMinusOperator = 'm';
    static readonly char unaryPlusOperator = 'p';

    public enum TokenType
    {
        None,
        Operand,
        Operator,
        LeftParenthesis,
        RightParenthesis,
    }

    public static void Test()
    {
        Assert.AreEqual(42f, EvaluateExpression("42"));
        Assert.AreEqual(42f, EvaluateExpression("[42]"));
        Assert.AreEqual(-42f, EvaluateExpression("-42"));
        Assert.AreEqual(-42f, EvaluateExpression("[-42]"));
        Assert.AreEqual(-42f, EvaluateExpression("-[42]"));
        Assert.AreEqual(42f, EvaluateExpression("-[-42]"));
        Assert.AreEqual(42f, EvaluateExpression("[-[-42]]"));
        Assert.AreEqual(-42f, EvaluateExpression("-[-[-42]]"));

        Assert.AreEqual(42f, EvaluateExpression("21+21"));
        Assert.AreEqual(-42f, EvaluateExpression("-21-21"));
        Assert.AreEqual(-42f, EvaluateExpression("[-21-21]"));
        Assert.AreEqual(42f, EvaluateExpression("21/-1x-2"));
        Assert.AreEqual(42f, EvaluateExpression("[-21-21]x-1"));
        Assert.AreEqual(42f, EvaluateExpression("[-21-21]x[-1]"));
        Assert.AreEqual(-42f, EvaluateExpression("[-21-21]x-[-1]"));
        Assert.AreEqual(42f, EvaluateExpression("-[-21-21]x-[-1]"));
        Assert.AreEqual(-42f, EvaluateExpression("-[-21-21]x-[-1x-42]/42"));
        Assert.AreEqual(42f, EvaluateExpression("-[-21-21]x-[-1x-42]/-42"));
    }

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
    public static float EvaluateExpression(string expression, float defaultValue = 0f)
    {
        Stack<float> values = new Stack<float>();
        Stack<char> operators = new Stack<char>();

        Action processOperator = () =>
        {
            if (IsUnaryOperator(operators.Peek()))
            {
                values.Push(operators.Pop() == unaryMinusOperator ? -values.Pop() : values.Pop());
            }
            else
            {
                values.Push(ComputeResult(values.Pop(), values.Pop(), operators.Pop()));
            }
        };

        int i = 0;
        TokenType lastTokenType = TokenType.None;
        while (i < expression.Length)
        {
            int j = 0;
            while (i + j < expression.Length && (char.IsNumber(expression[i + j]) || char.IsWhiteSpace(expression[i + j]) || expression[i + j] == '.' || expression[i + j] == ','))
            {
                j++;
            }

            if (j > 0)
            {
                values.Push(float.Parse(expression.Substring(i, j).Replace(" ", ""), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
                i += j;
                lastTokenType = TokenType.Operand;
            }

            if (i >= expression.Length)
            {
                break;
            }

            if (expression[i] == openSign)
            {
                operators.Push(openSign);
                lastTokenType = TokenType.LeftParenthesis;
            }
            else if (expression[i] == closeSign)
            {
                while (operators.Peek() != openSign)
                {
                    processOperator();
                }
                operators.Pop(); // Pop open parenthesis
                lastTokenType = TokenType.RightParenthesis;
            }
            else if (IsOperator(expression[i]))
            {
                if (lastTokenType != TokenType.Operand && lastTokenType != TokenType.RightParenthesis)
                {
                    if (expression[i] == '-' || expression[i] == '+')
                    {
                        operators.Push(expression[i] == '-' ? unaryMinusOperator : unaryPlusOperator);
                    }
                    else
                    {
                        // error
                    }
                }
                else
                {
                    while (operators.Count != 0
                        && (GetPrecedence(expression[i]) <= GetPrecedence(operators.Peek())
                            || (GetPrecedence(expression[i]) < GetPrecedence(operators.Peek()) && GetAssiociative(expression[i]) == AssociativeType.Right)))
                    {
                        processOperator();
                    }
                    operators.Push(expression[i]);
                    lastTokenType = TokenType.Operator;
                }
            }
            i++;
        }

        while (operators.Count != 0)
        {
            processOperator();
        }

        if (values.Count == 0)
        {
            return defaultValue;
        }

        return values.Pop();
    }

    static bool IsUnaryOperator(char op)
    {
        return (op == unaryMinusOperator || op == unaryPlusOperator);
    }

    public enum AssociativeType
    {
        None,
        Left,
        Right,
    }

    static AssociativeType GetAssiociative(char op)
    {
        if (op == 'm' || op == 'p')
        {
            return AssociativeType.Right;
        }
        else if (op == 'x' || op == '/')
        {
            return AssociativeType.Left;
        }
        else if (op == '+' || op == '-')
        {
            return AssociativeType.Left;
        }

        return AssociativeType.None;
    }

    static int GetPrecedence(char op)
    {
        if (op == 'm' || op == 'p')
        {
            return 6;
        }
        else if (op == 'x' || op == '/')
        {
            return 4;
        }
        else if (op == '+' || op == '-')
        {
            return 2;
        }

        return 0;
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

        if (op1 == 'm')
        {
            return false;
        }

        if ((op1 == 'x' || op1 == '/'/* || op2 == 'm'*/) && (op2 == '+' || op2 == '-'))
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
