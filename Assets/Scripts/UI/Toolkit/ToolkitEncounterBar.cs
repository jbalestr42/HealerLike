using UnityEngine;
using UnityEngine.UIElements;

// The header presents the game's room progress and delegates encounter controls to its HUD.
public class ToolkitEncounterBar
{
    ToolkitGameContext _context;
    ToolkitGameView _view;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        _context = context;
        _view = view;
    }

    // isPopoverOpen: the class screen's kit popover is the only detail the menu shows, so it keeps the panel up
    public void RefreshMenu(bool isPopoverOpen = false)
    {
        AddClass(_view.root.Q("hud-root"), "is-menu");
        AddClass(_view.root.Q(className: "speed-controls"), "is-hidden");
        _view.root.Q("field-toolbar")?.RemoveFromClassList(EmptyToolbarClass);
        _view.Show("menu-panel", true);
        _view.Show("spell-section", false);
        _view.Show("inventory-button", false);
        _view.Show("pause-button", false);
        _view.Show("currency-label", false);
        _view.Show("party-panel", false);
        _view.Show("detail-panel", isPopoverOpen);
        _view.Show("wave-button", false);
        _view.Show("start-button", true);
        _view.Show("sandbox-button", true);
        _view.SetText("phase-label", "WELCOME TO HEALERLIKE");
        // The menu screen carries the title and its one line of copy; the header says nothing over it
        _view.SetText("wave-label", "");
        _view.SetText("status-label", "Choose Start expedition to begin.");
        _view.SetButton("start-button", "Start expedition", true);
        _view.SetButton("sandbox-button", "Sandbox", true);
        _view.SetButton("inventory-button", null, false);
        _view.SetButton("pause-button", null, false);
    }

    public void Refresh(bool isStart, bool isPreparing, bool hasOverlay)
    {
        bool isAvailable = !_context.isPaused && !hasOverlay;
        bool isSandbox = _context.sandbox != null;
        // The toolbar keeps its height through the whole encounter, even with no button to show: a spell's Cancel
        // showing up in battle must not resize the free area the camera frames the board in
        _view.Show("field-toolbar", true);
        _view.Show("start-button", isStart);
        // On the menu it opens the sandbox, in the sandbox its tools
        _view.Show("sandbox-button", isSandbox && !isStart && !_context.hasInteraction);
        _view.SetButton("sandbox-button", "Sandbox", isAvailable);
        _view.Show("spell-section", !isStart);
        _view.Show("resource-bar", !isStart);
        _view.Show("wave-button", !isStart && (isPreparing || isSandbox) && !_context.hasInteraction);
        _view.Show("party-panel", !isStart);
        // The run starts with the scene (GameManager.Start) and a new one is reached from the end panel's "Return to
        // menu", so inside the gameplay scene this button is never one to press
        _view.SetButton("start-button", "Start expedition", false);
        _view.Show("currency-label", false);
        _view.SetText("wave-label", GetRoomText());
        _view.SetText("phase-label", GetPhaseText(isStart, isPreparing));
        _view.SetText("status-label", GetStatusText(isStart, isPreparing));
        if (isStart)
        {
            _view.SetButton("wave-button", "Start expedition", false);
        }
        else if (isSandbox)
        {
            _view.SetButton("wave-button", GetSandboxBattleText(_context.sandbox.isBattleRunning), isAvailable);
        }
        else
        {
            _view.SetButton(
                "wave-button",
                "Start battle",
                isAvailable && isPreparing && _context.ascension != null && _context.ascension.canStartBattle
            );
        }

        _view.SetButton("inventory-button", null, !isStart && isPreparing && !hasOverlay);
        // After the buttons of the other HUD parts (Cancel, the editor tools) were shown or hidden
        VisualElement toolbar = _view.root.Q("field-toolbar");
        toolbar?.EnableInClassList(EmptyToolbarClass, !HasVisibleAction(_view.root.Q("encounter-actions")));
    }

    public const string EmptyToolbarClass = "is-empty";

    // Whether one of the toolbar's buttons is shown
    public static bool HasVisibleAction(VisualElement actions)
    {
        if (actions == null)
        {
            return false;
        }

        foreach (VisualElement child in actions.Children())
        {
            if (!child.ClassListContains("is-hidden"))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetSandboxBattleText(bool isBattleRunning)
    {
        return isBattleRunning ? "Stop battle" : "Start battle";
    }

    public void RefreshMana()
    {
        Character character = _context.player.character;
        _view.Show("mana-gauge", character != null && character.mana != null);
        if (character != null && character.mana != null)
        {
            _view.SetText("mana-value", character.mana.Value.ToString("0"));
            VisualElement fill = _view.root.Q("mana-fill");
            if (fill != null)
            {
                fill.style.width = Length.Percent(ManaFraction(character.mana.Value, character.mana.Max) * 100f);
            }
        }
    }

    // The filled part of the mana gauge, empty when the healer has no mana to fill
    public static float ManaFraction(float value, float max)
    {
        return max > 0f ? Mathf.Clamp01(value / max) : 0f;
    }

    static void AddClass(VisualElement element, string className)
    {
        if (element != null)
        {
            element.AddToClassList(className);
        }
    }

    // The views that replace the battle text in the header, in the order they are checked
    static readonly GameScreen[] PhaseViews = { GameScreen.Map, GameScreen.Reward, GameScreen.Event };

    string GetRoomText()
    {
        if (_context.sandbox != null)
        {
            return "Sandbox";
        }

        if (_context.ascension == null)
        {
            return "Expedition";
        }

        return RoomText(_context.ascension.run);
    }

    public static string RoomText(RunState run)
    {
        return run == null || run.currentNode == null
            ? "Choose your route"
            : $"Room {run.currentFloor + 1} · {MapView.GetNodeLabel(run.currentNode.type)}";
    }

    GameScreen GetCurrentPhaseView()
    {
        foreach (GameScreen view in PhaseViews)
        {
            if (_context.IsCurrentView(view))
            {
                return view;
            }
        }

        return GameScreen.Game;
    }

    string GetPhaseText(bool isStart, bool isPreparing)
    {
        RunState run = _context.ascension != null ? _context.ascension.run : null;
        return PhaseText(GetCurrentPhaseView(), isStart, isPreparing, run != null && run.isOnBoss);
    }

    public static string PhaseText(GameScreen view, bool isStart, bool isPreparing, bool isOnBoss)
    {
        if (isStart)
        {
            return isOnBoss ? "SUMMIT REACHED" : "READY";
        }

        if (view == GameScreen.Map)
        {
            return "EXPEDITION MAP";
        }

        if (view == GameScreen.Reward)
        {
            return "CHOOSE A REWARD";
        }

        // Event rooms and the rest room share this view, so the text names neither; the room text beside it does
        if (view == GameScreen.Event)
        {
            return "MAKE A CHOICE";
        }

        return isPreparing ? "PREPARATION" : "IN BATTLE";
    }

    string GetStatusText(bool isStart, bool isPreparing)
    {
        return StatusText(
            GetCurrentPhaseView(),
            _context.isPaused,
            _context.hasInteraction,
            isStart,
            isPreparing,
            _view.isTouchLayout,
            Time.timeScale
        );
    }

    public static string StatusText(
        GameScreen view,
        bool isPaused,
        bool hasInteraction,
        bool isStart,
        bool isPreparing,
        bool isTouchLayout,
        float timeScale
    )
    {
        if (isPaused)
        {
            return "Paused";
        }

        if (hasInteraction)
        {
            return "Tap a target, or Cancel.";
        }

        if (isStart)
        {
            return "Start your expedition";
        }

        // The same fall-through the phase text had: an event or rest screen is not a battle
        if (view == GameScreen.Event)
        {
            return "Pick an option to continue.";
        }

        if (isPreparing)
        {
            return isTouchLayout
                ? "Open Party to deploy your allies."
                : "Deploy your party, distribute equipment, then start the battle.";
        }

        return $"Combat · {timeScale:0.#}× speed";
    }
}
