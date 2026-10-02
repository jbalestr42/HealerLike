using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Events
{

// The run side of an event room: ending the event goes back to the map, only while an event is played
public class EventRoomHostTests
{
    GameObject _go;
    AscensionGameType _gameType;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject();
        TestHelpers.WithLoggingDisabled(() => _gameType = _go.AddComponent<AscensionGameType>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    AscensionGameType.State state => TestHelpers.GetPrivateField<AscensionGameType.State>(_gameType, "_state");

    [Test]
    public void EndEvent_WhilePlayingAnEvent_GoesBackToTheMap()
    {
        TestHelpers.SetPrivateField(_gameType, "_state", AscensionGameType.State.PlayEvent);

        TestHelpers.WithLoggingDisabled(() => _gameType.EndEvent());

        Assert.AreEqual(AscensionGameType.State.ShowMap, state);
    }

    [Test]
    public void EndEvent_OutsideAnEvent_DoesNothing()
    {
        TestHelpers.SetPrivateField(_gameType, "_state", AscensionGameType.State.OnGoingBattle);

        _gameType.EndEvent();

        Assert.AreEqual(AscensionGameType.State.OnGoingBattle, state);
    }

    [Test]
    public void CloseBeforeEachChoice_ClosesThenApplies()
    {
        List<string> calls = new List<string>();
        List<EventChoice> choices = new List<EventChoice>
        {
            new EventChoice { label = "Heal", description = "Heals", onSelected = () => calls.Add("heal") },
            new EventChoice { label = "Resurrect", isAvailable = false, onSelected = () => calls.Add("resurrect") },
        };

        List<EventChoice> closing = AscensionGameType.CloseBeforeEachChoice(choices, () => calls.Add("close"));
        closing[0].onSelected();

        CollectionAssert.AreEqual(new[] { "close", "heal" }, calls);
        // Everything else is kept as is
        Assert.AreEqual("Heal", closing[0].label);
        Assert.AreEqual("Heals", closing[0].description);
        Assert.IsTrue(closing[0].isAvailable);
        Assert.IsFalse(closing[1].isAvailable);
    }

    [Test]
    public void CloseBeforeEachChoice_ChoiceWithoutAction_StillCloses()
    {
        int closed = 0;
        List<EventChoice> closing = AscensionGameType.CloseBeforeEachChoice(new List<EventChoice> { new EventChoice { label = "Leave" } }, () => closed++);

        closing[0].onSelected();

        Assert.AreEqual(1, closed);
    }
}

}
