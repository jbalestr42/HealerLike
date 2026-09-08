using NUnit.Framework;
using UnityEngine;

public class ExpressionEvaluatorTests
{
    [TestCase("42", 42f)]
    [TestCase("[42]", 42f)]
    [TestCase("-42", -42f)]
    [TestCase("[-42]", -42f)]
    [TestCase("-[42]", -42f)]
    [TestCase("-[-42]", 42f)]
    [TestCase("[-[-42]]", 42f)]
    [TestCase("-[-[-42]]", -42f)]
    [TestCase("21+21", 42f)]
    [TestCase("-21-21", -42f)]
    [TestCase("[-21-21]", -42f)]
    [TestCase("21/-1x-2", 42f)]
    [TestCase("[-21-21]x-1", 42f)]
    [TestCase("[-21-21]x[-1]", 42f)]
    [TestCase("[-21-21]x-[-1]", -42f)]
    [TestCase("-[-21-21]x-[-1]", 42f)]
    [TestCase("-[-21-21]x-[-1x-42]/42", -42f)]
    [TestCase("-[-21-21]x-[-1x-42]/-42", 42f)]
    public void Evaluate_ReturnsExpectedValue(string expression, float expected)
    {
        Assert.AreEqual(expected, ExpressionEvaluator.Evaluate(expression));
    }

    [Test]
    public void Evaluate_InvalidExpression_ReturnsDefaultValue()
    {
        // Every unknown character logs an error (expected, production behavior) - silence the
        // logger for this call only so it doesn't spam the Console during test runs.
        bool wasLogEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false;
        try
        {
            Assert.AreEqual(-1f, ExpressionEvaluator.Evaluate("not a number", -1f));
        }
        finally
        {
            Debug.unityLogger.logEnabled = wasLogEnabled;
        }
    }

    [Test]
    public void Evaluate_EmptyExpression_ReturnsDefaultValue()
    {
        Assert.AreEqual(7f, ExpressionEvaluator.Evaluate("", 7f));
    }
}
