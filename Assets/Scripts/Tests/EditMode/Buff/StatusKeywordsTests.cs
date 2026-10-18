using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// {status:Vulnerable} in a description: the title of the status, bold and colored, explained next to the text
public class StatusKeywordsTests
{
    readonly List<Object> _scriptableObjects = new List<Object>();
    IReadOnlyList<StatusData> _registeredStatuses;
    StatusData _vulnerable;
    StatusData _hasted;
    List<StatusData> _statuses;

    [SetUp]
    public void SetUp()
    {
        _registeredStatuses = StatusKeywords.statuses;
        _vulnerable = CreateStatus("Vulnerable", "Takes 15% more damage per stack", isPositive: false);
        _hasted = CreateStatus("Hasted", "Attacks 25% faster per stack", isPositive: true);
        _statuses = new List<StatusData> { _vulnerable, _hasted };
    }

    [TearDown]
    public void TearDown()
    {
        StatusKeywords.Register(_registeredStatuses);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    StatusData CreateStatus(string title, string description, bool isPositive)
    {
        StatusData status = ScriptableObject.CreateInstance<StatusData>();
        status.title = title;
        status.description = description;
        status.isPositive = isPositive;
        _scriptableObjects.Add(status);
        return status;
    }

    [Test]
    public void Format_Keyword_TheTitleBoldInTheColorOfADebuff()
    {
        string text = StatusKeywords.Format("Makes the target {status:Vulnerable}", _statuses);

        StringAssert.Contains($"<b><color={StatusKeywords.NegativeColor}>Vulnerable</color></b>", text);
        StringAssert.StartsWith("Makes the target ", text);
        StringAssert.DoesNotContain("{status:", text);
    }

    [Test]
    public void Format_PositiveStatus_InTheColorOfABuff()
    {
        string text = StatusKeywords.Format("{status:Hasted}", _statuses);

        StringAssert.Contains($"<color={StatusKeywords.PositiveColor}>Hasted</color>", text);
    }

    [Test]
    public void Format_BuffAndDebuff_NotTheSameColor()
    {
        Assert.AreNotEqual(StatusKeywords.PositiveColor, StatusKeywords.NegativeColor);
    }

    [Test]
    public void Format_Keyword_LinkedToItsStatus()
    {
        string text = StatusKeywords.Format("{status:Vulnerable}", _statuses);

        StringAssert.Contains("<link=\"status:Vulnerable\">", text);
        StringAssert.EndsWith("</link>", text);
    }

    [Test]
    public void Format_SeveralKeywords_EachFormatted()
    {
        string text = StatusKeywords.Format("{status:Hasted} then {status:Vulnerable}", _statuses);

        StringAssert.Contains(">Hasted<", text);
        StringAssert.Contains(">Vulnerable<", text);
        StringAssert.DoesNotContain("{status:", text);
    }

    [Test]
    public void Format_WithoutKeyword_Unchanged()
    {
        Assert.AreEqual("Heals 10 HP {data:value}", StatusKeywords.Format("Heals 10 HP {data:value}", _statuses));
    }

    [Test]
    public void Format_Null_Empty()
    {
        Assert.AreEqual("", StatusKeywords.Format(null, _statuses));
    }

    [Test]
    public void Format_UnknownStatus_ItsBareNameAndAnError()
    {
        string text = null;
        TestHelpers.WithLoggingDisabled(() => text = StatusKeywords.Format("Becomes {status:Frozen}", _statuses));

        Assert.AreEqual("Becomes Frozen", text);
    }

    [Test]
    public void Format_WithoutStatuses_UsesTheRegisteredOnes()
    {
        StatusKeywords.Register(_statuses);

        StringAssert.Contains(">Vulnerable<", StatusKeywords.Format("{status:Vulnerable}"));
    }

    [Test]
    public void GetCitedStatuses_EachOnceInTheirOrder()
    {
        string text = StatusKeywords.Format("{status:Vulnerable}, {status:Hasted}, {status:Vulnerable} again", _statuses);

        CollectionAssert.AreEqual(new[] { _vulnerable, _hasted }, StatusKeywords.GetCitedStatuses(text, _statuses));
    }

    [Test]
    public void GetCitedStatuses_PlainText_None()
    {
        CollectionAssert.IsEmpty(StatusKeywords.GetCitedStatuses("Vulnerable and Hasted, not as keywords", _statuses));
    }

    [Test]
    public void GetExplanation_TheTitleThenTheDescriptionOfEachStatus()
    {
        string text = StatusKeywords.Format("{status:Hasted} and {status:Vulnerable}", _statuses);

        string explanation = StatusKeywords.GetExplanation(text, _statuses);

        StringAssert.Contains($"<color={StatusKeywords.PositiveColor}>Hasted</color></b>\nAttacks 25% faster per stack", explanation);
        StringAssert.Contains($"<color={StatusKeywords.NegativeColor}>Vulnerable</color></b>\nTakes 15% more damage per stack", explanation);
        Assert.Less(explanation.IndexOf("Hasted"), explanation.IndexOf("Vulnerable"));
    }

    [Test]
    public void GetExplanation_WithoutKeyword_Empty()
    {
        Assert.AreEqual("", StatusKeywords.GetExplanation("Heals 10 HP", _statuses));
    }

    [Test]
    public void Find_ByTitle_NullWhenUnknown()
    {
        Assert.AreSame(_hasted, StatusKeywords.Find("Hasted", _statuses));
        Assert.IsNull(StatusKeywords.Find("Frozen", _statuses));
        Assert.IsNull(StatusKeywords.Find("Hasted", null));
    }
}

}
