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

    public void RefreshMenu()
    {
        AddClass(_view.root.Q("hud-root"), "is-menu");
        AddClass(_view.root.Q(className: "speed-controls"), "is-hidden");
        _view.Show("menu-panel", true);
        _view.Show("spell-section", false);
        _view.Show("inventory-button", false);
        _view.Show("pause-button", false);
        _view.Show("mark-entity-toggle", false);
        _view.Show("currency-label", false);
        _view.Show("party-panel", false);
        _view.Show("detail-panel", false);
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
        GameHUD hud = _context.legacy.gameHUD;
        bool isAvailable = !_context.isPaused && !hasOverlay;
        _view.Show("field-toolbar", isStart || isPreparing || _context.hasInteraction);
        _view.Show("start-button", isStart);
        _view.Show("sandbox-button", false);
        _view.Show("spell-section", !isStart);
        _view.Show("wave-button", !isStart && isPreparing && !_context.hasInteraction);
        _view.Show("party-panel", !isStart);
        _view.SetButton("start-button", "Start expedition", isAvailable && hud.startGameButton.interactable);
        _view.Show("currency-label", false);
        _view.SetText("wave-label", GetRoomText());
        _view.SetText("phase-label", GetPhaseText(isStart, isPreparing));
        _view.SetText("status-label", GetStatusText(isStart, isPreparing));
        if (isStart)
        {
            _view.SetButton("wave-button", "Start expedition", isAvailable && hud.startGameButton.interactable);
        }
        else
        {
            _view.SetButton(
                "wave-button",
                "Start battle",
                isAvailable && isPreparing && hud.nextWaveButton.interactable
            );
        }

        _view.SetButton("inventory-button", null, !isStart && isPreparing && !hasOverlay);
    }

    public void RefreshMana()
    {
        Character character = _context.player.character;
        _view.Show("mana-value", character != null && character.mana != null);
        if (character != null && character.mana != null)
        {
            _view.SetText("mana-value", character.mana.Value.ToString("0"));
        }
    }

    static void AddClass(VisualElement element, string className)
    {
        if (element != null)
        {
            element.AddToClassList(className);
        }
    }

    // The views that replace the battle text in the header, in the order they are checked
    static readonly ViewType[] PhaseViews = { ViewType.Map, ViewType.Upgrade, ViewType.Event };

    string GetRoomText()
    {
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

    ViewType GetCurrentPhaseView()
    {
        foreach (ViewType view in PhaseViews)
        {
            if (_context.IsCurrentView(view))
            {
                return view;
            }
        }

        return ViewType.Game;
    }

    string GetPhaseText(bool isStart, bool isPreparing)
    {
        RunState run = _context.ascension != null ? _context.ascension.run : null;
        return PhaseText(GetCurrentPhaseView(), isStart, isPreparing, run != null && run.isOnBoss);
    }

    public static string PhaseText(ViewType view, bool isStart, bool isPreparing, bool isOnBoss)
    {
        if (isStart)
        {
            return isOnBoss ? "SUMMIT REACHED" : "READY";
        }

        if (view == ViewType.Map)
        {
            return "EXPEDITION MAP";
        }

        if (view == ViewType.Upgrade)
        {
            return "CHOOSE A REWARD";
        }

        // Event rooms and the rest room share this view, so the text names neither; the room text beside it does
        if (view == ViewType.Event)
        {
            return "MAKE A CHOICE";
        }

        return isPreparing ? "PREPARATION" : "IN BATTLE";
    }

    string GetStatusText(bool isStart, bool isPreparing)
    {
        if (_context.isPaused)
        {
            return "Paused";
        }

        if (_context.hasInteraction)
        {
            return "Tap a target, or Cancel.";
        }

        if (isStart)
        {
            return "Start your expedition";
        }

        if (isPreparing)
        {
            return _view.isTouchLayout
                ? "Open Party to deploy your allies."
                : "Deploy your party, distribute equipment, then start the battle.";
        }

        return $"Combat · {Time.timeScale:0.#}× speed";
    }
}
