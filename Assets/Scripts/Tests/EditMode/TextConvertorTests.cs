using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TextConvertorTests
{
    class NestedData
    {
        public string Value = "nested-value";
    }

    class TestData
    {
        public int Number = 42;
        public NestedData Nested = new NestedData();
        public List<string> Items = new List<string> { "a", "b", "c" };
    }

    class FakeModifier : AttributeModifier
    {
        readonly float _value;
        public FakeModifier(float value) { _value = value; }
        public override float ApplyModifier() => _value;
    }

    static void WithLoggingDisabled(System.Action action)
    {
        bool wasLogEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false;
        try
        {
            action();
        }
        finally
        {
            Debug.unityLogger.logEnabled = wasLogEnabled;
        }
    }

    static Character CreateCharacterWithAttribute(AttributeType type, float value)
    {
        Character character = null;
        WithLoggingDisabled(() =>
        {
            GameObject go = new GameObject();
            AttributeManager attributeManager = go.AddComponent<AttributeManager>();
            // Awake() (which initializes AttributeManager's internal dictionary) isn't invoked
            // synchronously by AddComponent in EditMode tests - invoke it directly via reflection
            // rather than GameObject.SendMessage, which trips Unity's internal ShouldRunBehaviour() assert.
            typeof(AttributeManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(attributeManager, null);
            attributeManager.Add(type, new Attribute(value));

            // Adding Character triggers Unity's editor-only Reset() message, which NREs on
            // _buffManager since we deliberately skip the heavy Character.Init() for this test
            // double (it needs UIManager etc.) - only attributeManager is exercised here.
            character = go.AddComponent<Character>();
            character.attributeManager = attributeManager;
        });
        return character;
    }

    #region Convert - plain text / empty

    [Test]
    public void Convert_NullDescription_ReturnsEmptyString()
    {
        Assert.AreEqual("", TextConvertor.Convert(null, null, null));
    }

    [Test]
    public void Convert_EmptyDescription_ReturnsEmptyString()
    {
        Assert.AreEqual("", TextConvertor.Convert("", null, null));
    }

    [Test]
    public void Convert_PlainTextWithoutTokens_ReturnsUnchanged()
    {
        Assert.AreEqual("Deals damage to the target", TextConvertor.Convert("Deals damage to the target", null, null));
    }

    #endregion

    #region Convert - [expression]

    [Test]
    public void Convert_ExpressionInBrackets_IsEvaluatedAndReplaced()
    {
        string expected = ExpressionEvaluator.Evaluate("10+5").ToString(CultureInfo.InvariantCulture);
        Assert.AreEqual($"Deals {expected} damage", TextConvertor.Convert("Deals [10+5] damage", null, null));
    }

    [Test]
    public void Convert_ExpressionWithComma_IsTreatedAsDecimalSeparator()
    {
        // TextConvertor replaces ',' with '.' before evaluating, since ExpressionEvaluator itself
        // treats a bare comma as an unparseable decimal separator (see ExpressionEvaluatorTests).
        string result = TextConvertor.Convert("[10,5+1]", null, null);
        string expected = ExpressionEvaluator.Evaluate("10.5+1").ToString(CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void Convert_MultipleExpressions_EvaluatesEachIndependently()
    {
        string first = ExpressionEvaluator.Evaluate("1+1").ToString(CultureInfo.InvariantCulture);
        string second = ExpressionEvaluator.Evaluate("2+2").ToString(CultureInfo.InvariantCulture);

        Assert.AreEqual($"{first} and {second}", TextConvertor.Convert("[1+1] and [2+2]", null, null));
    }

    [Test]
    public void Convert_NestedBrackets_EvaluatesAsASingleExpression()
    {
        string expected = ExpressionEvaluator.Evaluate("[1+1]x2").ToString(CultureInfo.InvariantCulture);
        Assert.AreEqual(expected, TextConvertor.Convert("[[1+1]x2]", null, null));
    }

    #endregion

    #region Convert - {attribute:...}

    [Test]
    public void Convert_AttributeVariable_Base_ReturnsFormattedBaseValue()
    {
        Character character = CreateCharacterWithAttribute(AttributeType.HealthMax, 12.345f);

        Assert.AreEqual("12.35", TextConvertor.Convert("{attribute:base:HealthMax}", character, null));
        Object.DestroyImmediate(character.gameObject);
    }

    [Test]
    public void Convert_AttributeVariable_Current_ReturnsValueIncludingModifiers()
    {
        // Unlike "base" (which reads Attribute.BaseValue), "current" must reflect Attribute.Value,
        // i.e. the base value with modifiers applied - use a modifier to tell the two apart.
        Character character = CreateCharacterWithAttribute(AttributeType.HealthMax, 10f);
        Attribute attribute = character.attributeManager.Get(AttributeType.HealthMax);
        attribute.AddModifier(AttributeModifierType.Add, character.gameObject, new FakeModifier(5f));
        attribute.Update();

        Assert.AreEqual("10.00", TextConvertor.Convert("{attribute:base:HealthMax}", character, null));
        Assert.AreEqual("15.00", TextConvertor.Convert("{attribute:current:HealthMax}", character, null));
        Object.DestroyImmediate(character.gameObject);
    }

    [Test]
    public void Convert_AttributeVariable_NullCharacter_ReturnsDefaultValue()
    {
        Assert.AreEqual("1", TextConvertor.Convert("{attribute:base:HealthMax}", null, null));
    }

    [Test]
    public void Convert_AttributeVariable_MissingTypeToken_ReturnsDefaultValue()
    {
        Character character = CreateCharacterWithAttribute(AttributeType.HealthMax, 10f);

        string result = null;
        WithLoggingDisabled(() => result = TextConvertor.Convert("{attribute:base}", character, null));

        Assert.AreEqual("1", result);
        Object.DestroyImmediate(character.gameObject);
    }

    [Test]
    public void Convert_AttributeVariable_UnknownAttributeType_ReturnsDefaultValue()
    {
        Character character = CreateCharacterWithAttribute(AttributeType.HealthMax, 10f);

        string result = null;
        WithLoggingDisabled(() => result = TextConvertor.Convert("{attribute:base:NotAnAttributeType}", character, null));

        Assert.AreEqual("1", result);
        Object.DestroyImmediate(character.gameObject);
    }

    [Test]
    public void Convert_AttributeVariable_UnknownParam_ReturnsDefaultValue()
    {
        Character character = CreateCharacterWithAttribute(AttributeType.HealthMax, 10f);

        string result = null;
        WithLoggingDisabled(() => result = TextConvertor.Convert("{attribute:unknown:HealthMax}", character, null));

        Assert.AreEqual("1", result);
        Object.DestroyImmediate(character.gameObject);
    }

    #endregion

    #region Convert - {data:...}

    [Test]
    public void Convert_DataVariable_SimpleField_ReturnsFieldValue()
    {
        Assert.AreEqual("42", TextConvertor.Convert("{data:Number}", null, new TestData()));
    }

    [Test]
    public void Convert_DataVariable_NestedField_ReturnsNestedFieldValue()
    {
        Assert.AreEqual("nested-value", TextConvertor.Convert("{data:Nested.Value}", null, new TestData()));
    }

    [Test]
    public void Convert_DataVariable_ListIndex_ReturnsIndexedElement()
    {
        Assert.AreEqual("b", TextConvertor.Convert("{data:Items|1}", null, new TestData()));
    }

    [Test]
    public void Convert_DataVariable_NullData_ReturnsDefaultValue()
    {
        Assert.AreEqual("1", TextConvertor.Convert("{data:Number}", null, null));
    }

    [Test]
    public void Convert_DataVariable_UnknownField_ReturnsDefaultValue()
    {
        string result = null;
        WithLoggingDisabled(() => result = TextConvertor.Convert("{data:DoesNotExist}", null, new TestData()));

        Assert.AreEqual("1", result);
    }

    #endregion

    [Test]
    public void Convert_UnknownVariableType_ReturnsDefaultValue()
    {
        string result = null;
        WithLoggingDisabled(() => result = TextConvertor.Convert("{unknown:foo}", null, null));

        Assert.AreEqual("1", result);
    }

    [Test]
    public void Convert_VariableAndExpressionCombined_BothAreReplaced()
    {
        string expected = ExpressionEvaluator.Evaluate("1+1").ToString();
        Assert.AreEqual($"42 + {expected}", TextConvertor.Convert("{data:Number} + [1+1]", null, new TestData()));
    }

    #region GetPropertyValue

    [Test]
    public void GetPropertyValue_SimpleField_ReturnsValue()
    {
        Assert.AreEqual(42, TextConvertor.GetPropertyValue(new TestData(), "Number"));
    }

    [Test]
    public void GetPropertyValue_NestedField_ReturnsNestedValue()
    {
        Assert.AreEqual("nested-value", TextConvertor.GetPropertyValue(new TestData(), "Nested.Value"));
    }

    [Test]
    public void GetPropertyValue_ListIndex_ReturnsElementAtIndex()
    {
        Assert.AreEqual("c", TextConvertor.GetPropertyValue(new TestData(), "Items|2"));
    }

    [Test]
    public void GetPropertyValue_UnknownField_ReturnsNull()
    {
        Assert.IsNull(TextConvertor.GetPropertyValue(new TestData(), "DoesNotExist"));
    }

    [Test]
    public void GetPropertyValue_NullObject_ReturnsNull()
    {
        Assert.IsNull(TextConvertor.GetPropertyValue(null, "Number"));
    }

    #endregion
}
