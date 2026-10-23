using System.Collections.Generic;
using NUnit.Framework;

namespace Game
{

// The screens of the game opened over each other, the last one shown
public class GameScreensTests
{
    [Test]
    public void Current_NothingOpen_IsNone()
    {
        Assert.AreEqual(GameScreen.None, new GameScreens().current);
    }

    [Test]
    public void Open_ScreenOverTheBoard_IsCurrent()
    {
        GameScreens screens = new GameScreens();
        screens.Open(GameScreen.Game);

        screens.Open(GameScreen.Map);

        Assert.AreEqual(GameScreen.Map, screens.current);
        Assert.IsTrue(screens.IsOpen(GameScreen.Game));
    }

    [Test]
    public void Close_GoesBackToThePreviousScreen()
    {
        GameScreens screens = new GameScreens();
        screens.Open(GameScreen.Game);
        screens.Open(GameScreen.Map);

        screens.Close();

        Assert.AreEqual(GameScreen.Game, screens.current);
        Assert.IsFalse(screens.IsOpen(GameScreen.Map));
    }

    [Test]
    public void Close_TheBoardAlone_StaysOpen()
    {
        GameScreens screens = new GameScreens();
        screens.Open(GameScreen.Game);

        screens.Close();

        Assert.AreEqual(GameScreen.Game, screens.current);
    }

    [Test]
    public void OpenAndClose_TellWhichScreen()
    {
        GameScreens screens = new GameScreens();
        List<string> calls = new List<string>();
        screens.OnOpened.AddListener(screen => calls.Add("open " + screen));
        screens.OnClosed.AddListener(screen => calls.Add("close " + screen));
        screens.Open(GameScreen.Game);
        screens.Open(GameScreen.Event);

        screens.Close();

        CollectionAssert.AreEqual(new[] { "open Game", "open Event", "close Event" }, calls);
    }
}

}
