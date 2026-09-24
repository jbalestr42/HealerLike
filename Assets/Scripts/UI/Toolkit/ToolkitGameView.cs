using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

// Binds the semantic elements to models, all sizing, colors and layout live in the UXML and USS
public class ToolkitGameView
{
    public UnityEvent<ToolkitCardModel> OnInspect = new UnityEvent<ToolkitCardModel>();
    public UnityEvent OnInspectEnded = new UnityEvent();

    // Structural elements must not consume pointer events over the empty battlefield
    static readonly string[] structuralElements = { "hud-root", "main-content", "world-space", "game-ui", "hud" };

    VisualTreeAsset _cardTemplate = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
    Dictionary<string, List<ToolkitCard>> _lists = new Dictionary<string, List<ToolkitCard>>();

    readonly DataIconService _icons = new DataIconService();
    public DataIconService icons { get { return _icons; } }

    VisualElement _root;
    public VisualElement root { get { return _root; } }

    public ToolkitGameView(VisualElement root)
    {
        _root = root;
        _root.pickingMode = PickingMode.Ignore;
        foreach (string name in structuralElements)
        {
            VisualElement element = _root.Q(name);
            if (element != null)
            {
                element.pickingMode = PickingMode.Ignore;
            }
        }
    }

    public void SetText(string name, string value)
    {
        Label label = _root.Q<Label>(name);
        if (label != null && label.text != value)
        {
            label.text = value;
        }
    }

    public void Show(string name, bool show)
    {
        VisualElement element = _root.Q(name);
        if (element != null)
        {
            element.EnableInClassList("is-hidden", !show);
        }
    }

    public void SetButton(string name, string title, bool isEnabled)
    {
        Button button = _root.Q<Button>(name);
        if (button == null)
        {
            return;
        }

        if (title != null)
        {
            button.text = title;
        }

        button.SetEnabled(isEnabled);
    }

    public void AddClickListener(string name, System.Action action)
    {
        Button button = _root.Q<Button>(name);
        if (button != null)
        {
            button.clicked += action;
        }
    }

    public void SetResource(string name, float value, float maximum)
    {
        ProgressBar bar = _root.Q<ProgressBar>(name);
        if (bar == null)
        {
            return;
        }

        bar.value = ToolkitPresentation.Percentage(value, maximum);
        bar.title = ToolkitPresentation.Resource(value, maximum);
    }

    public void SetCards(string name, IReadOnlyList<ToolkitCardModel> models)
    {
        VisualElement parent = _root.Q(name);
        if (parent == null)
        {
            return;
        }

        List<ToolkitCard> cards;
        if (!_lists.TryGetValue(name, out cards))
        {
            cards = new List<ToolkitCard>();
            _lists.Add(name, cards);
        }

        while (cards.Count > models.Count)
        {
            cards[cards.Count - 1].button.RemoveFromHierarchy();
            cards.RemoveAt(cards.Count - 1);
        }

        for (int i = 0; i < models.Count; i++)
        {
            if (i == cards.Count)
            {
                ToolkitCard card = new ToolkitCard();
                card.Init(this, _cardTemplate);
                parent.Add(card.button);
                cards.Add(card);
            }

            cards[i].Refresh(models[i]);
        }
    }

    public void ShowDetail(ToolkitCardModel model)
    {
        if (model == null)
        {
            return;
        }

        SetText("detail-title", model.title);
        SetText("detail-description", model.description);
        VisualElement icon = _root.Q("detail-icon");
        if (icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(_icons.GetIcon(model.iconSource));
        }
    }

    // Destroys the icons this view generated
    public void Release()
    {
        _icons.Clear();
    }
}
