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
}

}
