using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

// {status:Blessed} in a description shown by UI Toolkit: the keyword of the status, as the legacy panels show it
public class ToolkitStatusTextTests
{
    readonly List<Object> _scriptableObjects = new List<Object>();
    IReadOnlyList<StatusData> _registeredStatuses;
    StatusData _blessed;
    StatusData _vulnerable;
    List<StatusData> _statuses;

    [SetUp]
    public void SetUp()
    {
        _registeredStatuses = StatusKeywords.statuses;
        _blessed = CreateStatus("Blessed", "Cleric units get stronger while Blessed", isPositive: true);
        _vulnerable = CreateStatus("Vulnerable", "Takes 15% more damage per stack", isPositive: false);
        _statuses = new List<StatusData> { _blessed, _vulnerable };
        StatusKeywords.Register(_statuses);
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
    public void Format_Keyword_TheTitleBoldInTheColorOfTheStatus()
    {
        string text = ToolkitStatusText.Format("Your spells make allies {status:Blessed}", _statuses);

        StringAssert.Contains($"<b><color={StatusKeywords.PositiveColor}>Blessed</color></b>", text);
        StringAssert.StartsWith("Your spells make allies ", text);
        StringAssert.DoesNotContain("{status:", text);
    }

    [Test]
    public void Format_NoKeyword_TheSameText()
    {
        string text = "+6 Heal Power";

        Assert.AreSame(text, ToolkitStatusText.Format(text, _statuses));
    }

    [Test]
    public void Format_Null_Empty()
    {
        Assert.AreEqual("", ToolkitStatusText.Format(null, _statuses));
    }

    [Test]
    public void WithExplanation_Keyword_TheTextThenWhatTheStatusDoes()
    {
        string text = ToolkitStatusText.WithExplanation("Makes the target {status:Vulnerable}", _statuses);

        int keyword = text.IndexOf("Makes the target ");
        int explanation = text.IndexOf("Takes 15% more damage per stack");
        Assert.AreEqual(0, keyword);
        Assert.Greater(explanation, keyword);
        StringAssert.DoesNotContain("{status:", text);
    }

    [Test]
    public void WithExplanation_NoKeyword_OnlyTheText()
    {
        Assert.AreEqual("Heals an ally", ToolkitStatusText.WithExplanation("Heals an ally", _statuses));
    }

    [Test]
    public void Card_DescriptionAndDetails_ShowTheKeyword()
    {
        using (ToolkitTestPanel panel = new ToolkitTestPanel())
        {
            VisualElement list = new VisualElement();
            list.name = "party-list";
            panel.root.Add(list);
            ToolkitGameView view = new ToolkitGameView(panel.root);
            view.SetCards("party-list", new[]
            {
                new ToolkitCardModel
                {
                    title = "Cleric", description = "Makes allies {status:Blessed}", details = "Starts with: {status:Blessed}"
                }
            });

            StringAssert.Contains(">Blessed</color>", list.Q<Label>("card-description").text);
            StringAssert.DoesNotContain("{status:", list.Q<Label>("card-description").text);
            StringAssert.DoesNotContain("{status:", list.Q<Label>("card-details").text);
            view.Release();
        }
    }

    [Test]
    public void ShowDetail_Keyword_ExplainsTheStatus()
    {
        VisualElement root = new VisualElement();
        Label description = new Label();
        description.name = "detail-description";
        root.Add(description);
        ToolkitGameView view = new ToolkitGameView(root);

        view.ShowDetail(new ToolkitCardModel { title = "Hexer", description = "Makes the target {status:Vulnerable}" });

        StringAssert.Contains(">Vulnerable</color>", description.text);
        StringAssert.Contains("Takes 15% more damage per stack", description.text);
        view.Release();
    }

    [Test]
    public void EntityInfoJoin_Keyword_ShowsTheKeyword()
    {
        string text = ToolkitEntityInfo.Join(new List<string> { "Makes the target {status:Vulnerable}" });

        StringAssert.Contains($"<color={StatusKeywords.NegativeColor}>Vulnerable</color>", text);
        StringAssert.DoesNotContain("{status:", text);
    }

    [Test]
    public void EventChoiceNormalise_Keyword_ShowsTheKeyword()
    {
        string text = ToolkitEventChoiceText.Normalise("Your units become {status:Blessed}");

        StringAssert.Contains(">Blessed</color>", text);
        StringAssert.DoesNotContain("{status:", text);
    }
}
}
