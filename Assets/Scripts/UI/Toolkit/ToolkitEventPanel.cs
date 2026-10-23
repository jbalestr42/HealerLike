using System.Collections.Generic;
using UnityEngine;

// The choices of an event or rest room, a card per choice of the event on screen
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
    // The event drawn, its cards are rebuilt only when it changes
    EventPrompt _shown;

    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view)
    {
        _gameUI = gameUI;
        _context = context;
        _view = view;
        _shown = null;
        _view.Show(PanelName, false);
    }

    public void Refresh()
    {
        EventPrompt prompt = _context.ascension != null && _context.IsCurrentView(GameScreen.Event)
            ? _context.ascension.currentEvent : null;
        if (prompt == _shown)
        {
            return;
        }

        _shown = prompt;
        if (prompt == null)
        {
            _view.Show(PanelName, false);
            return;
        }

        _view.SetText(TitleName, string.IsNullOrEmpty(prompt.title) ? DefaultTitle : prompt.title);
        string description = Format(prompt.description);
        _view.SetText(DescriptionName, description);
        _view.Show(DescriptionName, !string.IsNullOrEmpty(description));
        _view.SetCards(ListName, BuildModels(prompt.choices));
        _view.Show(PanelName, true);
    }

    // One card per choice, in their order
    public List<ToolkitCardModel> BuildModels(IReadOnlyList<EventChoice> choices)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (EventChoice choice in choices)
        {
            if (choice == null)
            {
                continue;
            }

            ToolkitCardModel model = new ToolkitCardModel();
            model.iconSource = choice.source != null ? choice.source : new ChoiceIcon(choice.label);
            model.title = choice.label;
            model.description = Format(choice.description);
            model.status = choice.isAvailable ? StatusOf(choice.source) : "Unavailable";
            model.isEnabled = choice.isAvailable;
            model.source = choice;
            model.activate = OnChoiceActivated;
            models.Add(model);
        }

        return models;
    }

    static string StatusOf(object source)
    {
        if (source is EntityData)
        {
            return "Unit · Choose";
        }

        return source is AItem ? "Item · Choose" : "Choose";
    }

    // Sizes are dropped: the card and the screen labels take their size from the theme
    static string Format(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ToolkitRichText.Normalise(ToolkitStatusText.Format(text), ToolkitRichTextOptions.StripSizes);
    }

    void OnChoiceActivated(ToolkitCardModel model)
    {
        if (_context.ascension == null)
        {
            return;
        }

        _context.ascension.SelectEventChoice(model.source as EventChoice);
        if (_gameUI != null)
        {
            _gameUI.Refresh();
        }
    }
}
