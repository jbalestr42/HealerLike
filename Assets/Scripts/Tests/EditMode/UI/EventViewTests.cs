using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

public class EventViewTests
{
    GameObject _go;
    GameObject _containerGo;
    EventView _view;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject();
        _containerGo = new GameObject("Choices", typeof(RectTransform));
        TestHelpers.WithLoggingDisabled(() => _view = _go.AddComponent<EventView>());
        TestHelpers.SetPrivateField(_view, "_choiceContainer", (RectTransform)_containerGo.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_containerGo);
    }

    static EventChoice Choice(string label, System.Action onSelected = null, bool isAvailable = true)
    {
        return new EventChoice { label = label, description = $"{label} description", isAvailable = isAvailable, onSelected = onSelected };
    }

    [Test]
    public void Display_OneButtonPerChoice()
    {
        _view.Display("Rest", "Take a break", new List<EventChoice> { Choice("Heal"), Choice("Resurrect") });

        Assert.AreEqual(2, _view.choiceButtons.Count);
        Assert.AreEqual(2, _containerGo.transform.childCount);
    }

    [Test]
    public void Display_AgainReplacesThePreviousChoices()
    {
        _view.Display("Rest", "", new List<EventChoice> { Choice("Heal"), Choice("Resurrect") });

        _view.Display("Library", "", new List<EventChoice> { Choice("Focus") });

        Assert.AreEqual(1, _view.choiceButtons.Count);
        Assert.AreEqual(1, _containerGo.transform.childCount);
    }

    [Test]
    public void Display_UnavailableChoiceCantBeClicked()
    {
        _view.Display("Rest", "", new List<EventChoice> { Choice("Heal"), Choice("Resurrect", isAvailable: false) });

        Assert.IsTrue(_view.choiceButtons[0].interactable);
        Assert.IsFalse(_view.choiceButtons[1].interactable);
    }

    [Test]
    public void Click_AppliesTheChoice()
    {
        int healed = 0;
        int resurrected = 0;
        _view.Display("Rest", "", new List<EventChoice> { Choice("Heal", () => healed++), Choice("Resurrect", () => resurrected++) });

        _view.choiceButtons[1].onClick.Invoke();

        Assert.AreEqual(0, healed);
        Assert.AreEqual(1, resurrected);
    }

    [Test]
    public void Select_OnlyTheFirstChoiceCounts()
    {
        int picked = 0;
        EventChoice heal = Choice("Heal", () => picked++);
        EventChoice resurrect = Choice("Resurrect", () => picked++);
        _view.Display("Rest", "", new List<EventChoice> { heal, resurrect });

        _view.Select(heal);
        _view.Select(heal);
        _view.Select(resurrect);

        Assert.AreEqual(1, picked);
    }

    [Test]
    public void Select_UnavailableChoice_DoesNothingAndKeepsTheOthersPickable()
    {
        int healed = 0;
        int resurrected = 0;
        EventChoice heal = Choice("Heal", () => healed++);
        EventChoice resurrect = Choice("Resurrect", () => resurrected++, isAvailable: false);
        _view.Display("Rest", "", new List<EventChoice> { heal, resurrect });

        _view.Select(resurrect);
        _view.Select(heal);

        Assert.AreEqual(0, resurrected);
        Assert.AreEqual(1, healed);
    }

    [Test]
    public void Display_AgainAllowsANewChoice()
    {
        int picked = 0;
        EventChoice heal = Choice("Heal", () => picked++);
        _view.Display("Rest", "", new List<EventChoice> { heal });
        _view.Select(heal);

        _view.Display("Rest", "", new List<EventChoice> { heal });
        _view.Select(heal);

        Assert.AreEqual(2, picked);
    }

    [Test]
    public void GetChoiceText_LabelInBoldThenTheDescription()
    {
        Assert.AreEqual("<b>Heal</b>\n<size=75%>Heals every unit</size>", EventView.GetChoiceText(new EventChoice { label = "Heal", description = "Heals every unit" }));
        Assert.AreEqual("<b>Leave</b>", EventView.GetChoiceText(new EventChoice { label = "Leave" }));
    }
}

}
