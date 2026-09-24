using UnityEngine;
using UnityEngine.UIElements;

// The header: encounter phase, wave, status line, gold, mana and the start and wave buttons
public class ToolkitWaveBar
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
        _view.SetText("phase-label", "WELCOME TO HEALERLIKE");
        _view.SetText("wave-label", "Build your party. Keep them alive.");
        _view.SetText("status-label", "Choose Start expedition to begin.");
        _view.SetButton("start-button", "Start expedition", true);
        _view.SetButton("inventory-button", null, false);
        _view.SetButton("pause-button", null, false);
    }

    public void Refresh(bool isStart, bool isPreparing, bool hasOverlay)
    {
        GameHUD hud = _context.legacy.gameHUD;
        bool isAvailable = !_context.isPaused && !hasOverlay;
        _view.Show("start-button", isStart);
        _view.Show("wave-button", !isStart);
        _view.SetButton("start-button", "Start expedition", isAvailable && hud.startGameButton.interactable);
        _view.SetText("currency-label", $"{_context.player.gold} gold");
        _view.SetText("wave-label", GetWaveText());
        _view.SetText("phase-label", GetPhaseText(isStart, isPreparing));
        _view.SetText("status-label", GetStatusText(isStart, isPreparing));
        if (isStart)
        {
            _view.SetButton("wave-button", "Start expedition", isAvailable && hud.startGameButton.interactable);
        }
        else
        {
            _view.SetButton("wave-button", "Start wave", isAvailable && isPreparing && hud.nextWaveButton.interactable);
        }

        _view.SetButton("inventory-button", null, !isStart && isPreparing && !hasOverlay);
    }

    public void RefreshMana()
    {
        Character character = _context.player.character;
        if (character != null && character.mana != null)
        {
            _view.SetResource("mana-bar", character.mana.Value, character.mana.Max);
        }
    }

    static void AddClass(VisualElement element, string className)
    {
        if (element != null)
        {
            element.AddToClassList(className);
        }
    }

    string GetWaveText()
    {
        if (_context.ascension == null)
        {
            return "Expedition";
        }

        return $"Wave {_context.ascension.currentRound}";
    }

    string GetPhaseText(bool isStart, bool isPreparing)
    {
        if (isStart)
        {
            return "READY";
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
            return "Click a valid target. Escape cancels.";
        }

        if (isStart)
        {
            return "Start your expedition";
        }

        if (isPreparing)
        {
            return "Deploy your party, distribute equipment, then start the wave.";
        }

        return $"Combat · {Time.timeScale:0.#}× speed";
    }
}
