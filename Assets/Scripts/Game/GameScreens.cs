using System.Collections.Generic;
using UnityEngine.Events;

// The screens of the game, whatever draws them
public enum GameScreen
{
    None,
    // The board: arranging the units and fighting
    Game,
    // The choice of a reward after a fight or in a treasure room
    Reward,
    // The end of the run, won or lost
    GameOver,
    // The run map, to pick the next room or to look at it while arranging the units
    Map,
    // The choices of an event or rest room
    Event,
}

// The screens opened over each other: the last one opened is shown, closing it goes back to the previous one
public class GameScreens
{
    public UnityEvent<GameScreen> OnOpened = new UnityEvent<GameScreen>();
    public UnityEvent<GameScreen> OnClosed = new UnityEvent<GameScreen>();

    List<GameScreen> _stack = new List<GameScreen>();

    public GameScreen current
    {
        get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : GameScreen.None; }
    }

    public bool IsOpen(GameScreen screen)
    {
        return _stack.Contains(screen);
    }

    public void Open(GameScreen screen)
    {
        _stack.Add(screen);
        OnOpened.Invoke(screen);
    }

    // The screen on top, never the last one left (the board stays under every other screen)
    public void Close()
    {
        if (_stack.Count <= 1)
        {
            return;
        }

        GameScreen closed = current;
        _stack.RemoveAt(_stack.Count - 1);
        OnClosed.Invoke(closed);
    }
}
