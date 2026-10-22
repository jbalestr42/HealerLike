using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{

// The Toolkit menu has no DataManager: the game UI registers the statuses of its game data for the {status:X} texts
public class ToolkitGameUIStatusesTests
{
    IReadOnlyList<StatusData> _registeredStatuses;
    GameObject _gameObject;
    GameData _gameData;
    StatusData _blessed;

    [SetUp]
    public void SetUp()
    {
        _registeredStatuses = StatusKeywords.statuses;
        _blessed = ScriptableObject.CreateInstance<StatusData>();
        _blessed.title = "Blessed";
        _gameData = ScriptableObject.CreateInstance<GameData>();
        _gameData.statuses = new List<StatusData> { _blessed };
        _gameObject = new GameObject("ToolkitGameUI");
    }

    [TearDown]
    public void TearDown()
    {
        StatusKeywords.Register(_registeredStatuses);
        Object.DestroyImmediate(_gameObject);
        Object.DestroyImmediate(_gameData);
        Object.DestroyImmediate(_blessed);
    }

    [Test]
    public void RegisterStatuses_RegistersTheStatusesOfItsGameData()
    {
        StatusKeywords.Register(new List<StatusData>());
        ToolkitGameUI gameUI = _gameObject.AddComponent<ToolkitGameUI>();
        gameUI.gameData = _gameData;

        TestHelpers.InvokePrivate(gameUI, "RegisterStatuses");

        Assert.AreSame(_blessed, StatusKeywords.Find("Blessed", StatusKeywords.statuses));
    }

    [Test]
    public void RegisterStatuses_WithoutGameData_KeepsTheRegisteredStatuses()
    {
        List<StatusData> registered = new List<StatusData> { _blessed };
        StatusKeywords.Register(registered);
        ToolkitGameUI gameUI = _gameObject.AddComponent<ToolkitGameUI>();

        TestHelpers.InvokePrivate(gameUI, "RegisterStatuses");

        Assert.AreSame(registered, StatusKeywords.statuses);
    }
}

}
