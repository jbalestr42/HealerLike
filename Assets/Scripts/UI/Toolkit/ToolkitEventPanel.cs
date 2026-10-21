using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UiButton = UnityEngine.UI.Button;

// The choices of an event or rest room, drawn as Toolkit cards instead of Julien's runtime built buttons.
// His EventView keeps running underneath: it owns the choices and their effects, this panel only reads the
// buttons it created and presses them. His canvas stays the fallback: it is hidden only while this panel has at
// least one activatable card, so a choice we cannot draw never leaves the run with nothing to press. ToolkitGameUI
// refreshes this panel ahead of its own early returns, and ToolkitLegacyCanvases leaves his canvas on until Take.
public class ToolkitEventPanel
{
    public static readonly string PanelName = "event-panel";
    public static readonly string ListName = "event-list";
    public static readonly string TitleName = "event-title";
    public static readonly string DescriptionName = "event-description";
    public static readonly string DefaultTitle = "Choose";

    // The icon of a choice with no asset behind it: a procedural data icon labelled with the choice
    public class ChoiceIcon : IDataIconMetadata
    {
        readonly string _label;

        public ChoiceIcon(string label)
        {
            _label = label;
        }

        public string label { get { return _label; } }
        public Sprite icon { get { return null; } }
    }

    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitLegacyCanvases _legacy;
    bool _reportedEmpty;
    bool _isDrawing;
    bool _hasScreen;
    string _title;
    string _description;
    readonly List<UiButton> _buttons = new List<UiButton>();
    readonly List<bool> _availability = new List<bool>();

    // Finds the game asset a choice label names. Overridden by tests, by default the game data of the managers
    public Func<string, object> findAsset;

    // True while the Toolkit cards are the ones on screen
    public bool isDrawing { get { return _isDrawing; } }

    // legacy is null when the host keeps Julien's canvases visible: the panel then draws nothing
    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view, ToolkitLegacyCanvases legacy)
    {
        _gameUI = gameUI;
        _context = context;
        _view = view;
        _legacy = legacy;
        _reportedEmpty = false;
        _isDrawing = false;
        _hasScreen = false;
        _buttons.Clear();
        _availability.Clear();
        _view.Show(PanelName, false);
    }

    public void Refresh()
    {
        if (_legacy == null || _context.ui == null)
        {
            return;
        }

        if (LegacyUiReader.CurrentView(_context.ui) != ViewType.Event)
        {
            Leave();
            return;
        }

        EventView eventView = _context.ui.GetView<EventView>(ViewType.Event);
        IReadOnlyList<UiButton> choices = LegacyUiReader.EventChoices(eventView);
        string title = ToolkitEventChoiceText.Normalise(LegacyUiReader.EventTitle(eventView));
        string description = ToolkitEventChoiceText.Normalise(LegacyUiReader.EventDescription(eventView));
        if (_hasScreen && IsUnchanged(choices, title, description))
        {
            return;
        }

        Remember(choices, title, description);
        List<ToolkitCardModel> models = BuildModels(choices);
        if (CountActivatable(models) == 0)
        {
            FallBack(choices);
            return;
        }

        Take(models, title, description);
    }

    // One card per choice button of his view, in his order
    public List<ToolkitCardModel> BuildModels(IReadOnlyList<UiButton> choices)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (UiButton button in choices)
        {
            if (button == null)
            {
                continue;
            }

            ToolkitCardModel model = BuildModel(button);
            if (model != null)
            {
                models.Add(model);
            }
        }

        return models;
    }

    ToolkitCardModel BuildModel(UiButton button)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        string raw = text != null ? text.text : string.Empty;
        string label = ToolkitEventChoiceText.Label(button.gameObject.name, raw);
        if (string.IsNullOrEmpty(label))
        {
            return null;
        }

        object asset = FindAssetOf(label);
        bool isEnabled = button.interactable;
        ToolkitCardModel model = new ToolkitCardModel();
        model.iconSource = asset != null ? asset : new ChoiceIcon(label);
        model.title = label;
        model.description = ToolkitEventChoiceText.Description(label, raw);
        model.status = isEnabled ? StatusOf(asset) : "Unavailable";
        model.isEnabled = isEnabled;
        model.source = button;
        model.activate = OnChoiceActivated;
        return model;
    }

    static string StatusOf(object asset)
    {
        if (asset is EntityData)
        {
            return "Unit · Choose";
        }

        return asset is AItem ? "Item · Choose" : "Choose";
    }

    // The real asset behind a label, found by title: the dead allies of the run and the units the class can
    // recruit, then the items of the game data. Null for the labels with no asset (Heal, Leave, Continue).
    object FindAssetOf(string label)
    {
        return findAsset != null ? findAsset(label) : FindManagedAsset(label);
    }

    object FindManagedAsset(string label)
    {
        if (_context.ascension != null && _context.ascension.run != null)
        {
            EntityData dead = FindUnit(_context.ascension.run.deadAllies, label);
            if (dead != null)
            {
                return dead;
            }
        }

        DataManager manager = DataManager.existingInstance;
        if (manager == null || manager.data == null)
        {
            return null;
        }

        if (_context.player != null && _context.player.character != null)
        {
            EntityData recruit = FindUnit(manager.GetRewardEntities(_context.player.character.data), label);
            if (recruit != null)
            {
                return recruit;
            }
        }

        if (manager.data.items != null)
        {
            foreach (AItemFactory factory in manager.data.items)
            {
                if (factory != null && factory.title == label)
                {
                    return factory.GetItem();
                }
            }
        }

        return null;
    }

    static EntityData FindUnit(IReadOnlyList<EntityData> units, string label)
    {
        if (units == null)
        {
            return null;
        }

        foreach (EntityData unit in units)
        {
            if (unit != null && unit.title == label)
            {
                return unit;
            }
        }

        return null;
    }

    static int CountActivatable(List<ToolkitCardModel> models)
    {
        int count = 0;
        foreach (ToolkitCardModel model in models)
        {
            if (model.isEnabled && model.activate != null)
            {
                count++;
            }
        }

        return count;
    }

    // Our cards go up, his canvas goes down
    void Take(List<ToolkitCardModel> models, string title, string description)
    {
        _reportedEmpty = false;
        _isDrawing = true;
        _view.SetText(TitleName, string.IsNullOrEmpty(title) ? DefaultTitle : title);
        _view.SetText(DescriptionName, description);
        _view.Show(DescriptionName, !string.IsNullOrEmpty(description));
        _view.SetCards(ListName, models);
        _view.Show(PanelName, true);
        _legacy.SetEventScreenVisible(false);
    }

    // No card can be pressed: his canvas is the only way out of the room, so it stays on screen
    void FallBack(IReadOnlyList<UiButton> choices)
    {
        _isDrawing = false;
        _view.SetCards(ListName, new List<ToolkitCardModel>());
        _view.Show(PanelName, false);
        _legacy.SetEventScreenVisible(true);
        if (choices.Count == 0 || _reportedEmpty)
        {
            return;
        }

        _reportedEmpty = true;
        List<string> names = new List<string>();
        foreach (UiButton button in choices)
        {
            names.Add(button != null ? button.gameObject.name : "destroyed");
        }

        Debug.LogError($"[ToolkitEventPanel] Julien's event screen offers {choices.Count} choice(s) and none became an "
            + $"activatable card, his canvas stays on screen as the fallback: {string.Join("; ", names)}");
    }

    // Off his event screen, his canvas goes back to what the scene made of it, the state it had before our HUD
    void Leave()
    {
        _reportedEmpty = false;
        _isDrawing = false;
        _hasScreen = false;
        _buttons.Clear();
        _availability.Clear();
        _view.Show(PanelName, false);
        _legacy.SetEventScreenVisible(true);
    }

    bool IsUnchanged(IReadOnlyList<UiButton> choices, string title, string description)
    {
        if (choices.Count != _buttons.Count || title != _title || description != _description)
        {
            return false;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] == null || choices[i] != _buttons[i] || choices[i].interactable != _availability[i])
            {
                return false;
            }
        }

        return true;
    }

    void Remember(IReadOnlyList<UiButton> choices, string title, string description)
    {
        _hasScreen = true;
        _title = title;
        _description = description;
        _buttons.Clear();
        _availability.Clear();
        foreach (UiButton button in choices)
        {
            _buttons.Add(button);
            _availability.Add(button != null && button.interactable);
        }
    }

    // His button runs his own Select: first pick only, then his callback closes the view and applies the choice
    void OnChoiceActivated(ToolkitCardModel model)
    {
        UiButton button = model.source as UiButton;
        if (button == null || !button.interactable)
        {
            Debug.LogWarning("[ToolkitEventPanel] The choice behind the card is gone or unavailable, nothing was invoked");
            return;
        }

        button.onClick.Invoke();
        if (_gameUI != null)
        {
            _gameUI.Refresh();
        }
    }
}
