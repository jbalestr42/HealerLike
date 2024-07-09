using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Assertions;

public class ExpressionEvaluator
{
    public static char openParenthesis = '[';
    public static char closeParenthesis = ']';

    public enum TokenType
    {
        None,
        Operand,
        Operator,
        LeftParenthesis,
        RightParenthesis,
    }

    public enum AssociativeType
    {
        None,
        Left,
        Right,
    }

    public class OperatorData
    {
        public char op;
        public AssociativeType associative;
        public int precedence;
        public bool isUnary;
        public Func<float, float, float> eval;

        public OperatorData(char op, AssociativeType associative, int precedence, bool isUnary, Func<float, float, float> eval)
        {
            this.op = op;
            this.associative = associative;
            this.precedence = precedence;
            this.isUnary = isUnary;
            this.eval = eval;
        }
    }

    static Dictionary<char, OperatorData> operatorData = new Dictionary<char, OperatorData>();
    static Dictionary<char, char> unaryConvertor = new Dictionary<char, char>();

    static ExpressionEvaluator()
    {
        operatorData['p'] = new OperatorData('p', AssociativeType.Right, 6, true, (float left, float right) => left);
        operatorData['m'] = new OperatorData('m', AssociativeType.Right, 6, true, (float left, float right) => -left);
        operatorData['x'] = new OperatorData('x', AssociativeType.Left, 4, false, (float left, float right) => right * left);
        operatorData['/'] = new OperatorData('/', AssociativeType.Left, 4, false, (float left, float right) => right / left);
        operatorData['+'] = new OperatorData('+', AssociativeType.Left, 2, false, (float left, float right) => right + left);
        operatorData['-'] = new OperatorData('-', AssociativeType.Left, 2, false, (float left, float right) => right - left);
        operatorData['['] = new OperatorData('[', AssociativeType.None, 0, false, null);
        operatorData[']'] = new OperatorData(']', AssociativeType.None, 0, false, null);

        unaryConvertor['+'] = 'p';
        unaryConvertor['-'] = 'm';
    }

    /// <summary>
    /// Resolve expression of type [[AxB]+C]
    /// Support unary expression
    /// Support floating point
    /// </summary>
    /// <returns>Returns evaluated expression as float</returns>
    public static float Evaluate(string expression, float defaultValue = 0f)
    {
        Stack<float> values = new Stack<float>();
        Stack<char> operators = new Stack<char>();

        Action processOperator = () =>
        {
            OperatorData op = operatorData[operators.Pop()];
            values.Push(op.eval(values.Pop(), op.isUnary ? 0f : values.Pop()));
        };

        int i = 0;
        TokenType lastTokenType = TokenType.None;
        while (i < expression.Length)
        {
            int j = 0;
            // TODO improve this: only one dot/comma
            // TODO use a proper tokenizer?
            while (i + j < expression.Length && (char.IsNumber(expression[i + j]) || char.IsWhiteSpace(expression[i + j]) || expression[i + j] == '.' || expression[i + j] == ','))
            {
                j++;
            }

            if (j > 0) // Push the numeric value
            {
                string subExpression = expression.Substring(i, j).Replace(" ", "");
                if (float.TryParse(subExpression, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float value))
                {
                    values.Push(value);
                }
                else
                {
                    LogError($"Failed to parse float '{subExpression}'");
                    return defaultValue;
                }
                i += j;
                lastTokenType = TokenType.Operand;

                if (i >= expression.Length)
                {
                    break;
                }
            }

            if (expression[i] == openParenthesis) // Push '('
            {
                operators.Push(openParenthesis);
                lastTokenType = TokenType.LeftParenthesis;
            }
            else if (expression[i] == closeParenthesis) // Process operators until we found a '(' on the stack
            {
                while (operators.Peek() != openParenthesis)
                {
                    processOperator();
                }
                operators.Pop(); // Pop '('
                lastTokenType = TokenType.RightParenthesis;
            }
            else if (operatorData[expression[i]].precedence > 0) // Push an operator
            {
                if (lastTokenType != TokenType.Operand && lastTokenType != TokenType.RightParenthesis) // It's an unary operator
                {
                    if (unaryConvertor.ContainsKey(expression[i]))
                    {
                        operators.Push(unaryConvertor[expression[i]]);
                    }
                    else
                    {
                        LogError($"Bad unary operator '{expression[i]}'");
                        return defaultValue;
                    }
                }
                else // It's a binary operator
                {
                    // Process all operators on the stack until the new operator precedence is higher, then push the new operator
                    OperatorData op = operatorData[expression[i]];
                    while (operators.Count != 0 && (op.precedence <= operatorData[operators.Peek()].precedence
                                                || (op.precedence < operatorData[operators.Peek()].precedence && op.associative == AssociativeType.Right)))
                    {
                        processOperator();
                    }

                    operators.Push(expression[i]);
                    lastTokenType = TokenType.Operator;
                }
            }
            else
            {
                LogError($"We should never reach this code.");
            }
            i++;
        }

        while (operators.Count != 0)
        {
            processOperator();
        }

        return values.Count == 0 ? defaultValue : values.Pop();
    }

    static void LogError(string error)
    {
        UnityEngine.Debug.LogError($"[ERROR] {error}");
    } 

    public static void Test()
    {
        Assert.AreEqual(42f, Evaluate("42"));
        Assert.AreEqual(42f, Evaluate("[42]"));
        Assert.AreEqual(-42f, Evaluate("-42"));
        Assert.AreEqual(-42f, Evaluate("[-42]"));
        Assert.AreEqual(-42f, Evaluate("-[42]"));
        Assert.AreEqual(42f, Evaluate("-[-42]"));
        Assert.AreEqual(42f, Evaluate("[-[-42]]"));
        Assert.AreEqual(-42f, Evaluate("-[-[-42]]"));

        Assert.AreEqual(42f, Evaluate("21+21"));
        Assert.AreEqual(-42f, Evaluate("-21-21"));
        Assert.AreEqual(-42f, Evaluate("[-21-21]"));
        Assert.AreEqual(42f, Evaluate("21/-1x-2"));
        Assert.AreEqual(42f, Evaluate("[-21-21]x-1"));
        Assert.AreEqual(42f, Evaluate("[-21-21]x[-1]"));
        Assert.AreEqual(-42f, Evaluate("[-21-21]x-[-1]"));
        Assert.AreEqual(42f, Evaluate("-[-21-21]x-[-1]"));
        Assert.AreEqual(-42f, Evaluate("-[-21-21]x-[-1x-42]/42"));
        Assert.AreEqual(42f, Evaluate("-[-21-21]x-[-1x-42]/-42"));

        UnityEngine.Debug.Log("All tests passed.");
    }
}
