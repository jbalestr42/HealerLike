using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        // Every unknown character logs an error; only assert the first to stay resilient to the exact text.
        LogAssert.Expect(LogType.Error, "[ERROR] Unknown character 'n' in expression 'not a number'");
        LogAssert.ignoreFailingMessages = true;

        Assert.AreEqual(-1f, ExpressionEvaluator.Evaluate("not a number", -1f));

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void Evaluate_EmptyExpression_ReturnsDefaultValue()
    {
        Assert.AreEqual(7f, ExpressionEvaluator.Evaluate("", 7f));
    }
}
