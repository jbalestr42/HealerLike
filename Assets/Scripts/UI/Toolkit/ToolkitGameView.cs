using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace HealerLike.UI.Toolkit
{
    /// <summary>Binds semantic elements to models. All sizing, colors and layout live in UXML/USS.</summary>
    public sealed class ToolkitGameView
    {
        public VisualElement Root { get; }
        readonly VisualTreeAsset _cardTemplate = UnityEngine.Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
        readonly Dictionary<string, List<Card>> _lists = new Dictionary<string, List<Card>>();
        public Action<ToolkitCardModel> Inspect;
        public Action InspectEnded;

        sealed class Card
        {
            public ToolkitCardModel Model;
            public Button Button;
            public VisualElement Icon;
            public Label Title, Description, Status;
            public object IconSource;
            public bool HasIcon;
        }

        public ToolkitGameView(VisualElement root)
        {
            Root = root;
            Root.pickingMode = PickingMode.Ignore;
            // Structural elements must not consume pointer events over the empty battlefield.
            foreach (string name in new[] { "hud-root", "main-content", "world-space", "game-ui", "hud" })
            {
                var element = Root.Q(name);
                if (element != null) element.pickingMode = PickingMode.Ignore;
            }
        }

        public void Text(string name, string value)
        {
            var label = Root.Q<Label>(name);
            if (label != null && label.text != value) label.text = value;
        }

        public void Visible(string name, bool visible)
        {
            Root.Q(name)?.EnableInClassList("is-hidden", !visible);
        }

        public void Button(string name, string title, bool enabled)
        {
            var button = Root.Q<Button>(name);
            if (button == null) return;
            if (title != null) button.text = title;
            button.SetEnabled(enabled);
        }

        public void Bind(string name, Action action)
        {
            var button = Root.Q<Button>(name);
            if (button != null) button.clicked += action;
        }

        public void Resource(string name, float value, float maximum)
        {
            var bar = Root.Q<ProgressBar>(name);
            if (bar == null) return;
            bar.value = ToolkitPresentation.Percentage(value, maximum);
            bar.title = ToolkitPresentation.Resource(value, maximum);
        }

        public void Cards(string name, IReadOnlyList<ToolkitCardModel> models)
        {
            var parent = Root.Q(name);
            if (parent == null) return;
            if (!_lists.TryGetValue(name, out var cards))
            {
                cards = new List<Card>();
                _lists.Add(name, cards);
            }
            while (cards.Count > models.Count)
            {
                cards[cards.Count - 1].Button.RemoveFromHierarchy();
                cards.RemoveAt(cards.Count - 1);
            }
            for (int i = 0; i < models.Count; i++)
            {
                if (i == cards.Count)
                {
                    var template = _cardTemplate != null ? _cardTemplate.CloneTree() : null;
                    var card = new Card { Button = template?.Q<Button>("data-card") ?? new Button(),
                        Icon = template?.Q("card-icon") ?? new VisualElement(),
                        Title = template?.Q<Label>("card-title") ?? new Label(),
                        Description = template?.Q<Label>("card-description") ?? new Label(),
                        Status = template?.Q<Label>("card-status") ?? new Label() };
                    card.Button.AddToClassList("data-card");
                    card.Icon.AddToClassList("data-card__icon");
                    card.Title.AddToClassList("data-card__title");
                    card.Description.AddToClassList("data-card__description");
                    card.Status.AddToClassList("data-card__status");
                    if (template == null)
                    {
                        card.Button.Add(card.Icon);
                        card.Button.Add(card.Title);
                        card.Button.Add(card.Description);
                        card.Button.Add(card.Status);
                    }
                    card.Button.clicked += () => { if (card.Model.Enabled) card.Model.Activate?.Invoke(); };
                    card.Button.RegisterCallback<PointerEnterEvent>(_ => Inspect?.Invoke(card.Model));
                    card.Button.RegisterCallback<FocusInEvent>(_ => Inspect?.Invoke(card.Model));
                    card.Button.RegisterCallback<PointerLeaveEvent>(_ => InspectEnded?.Invoke());
                    card.Button.RegisterCallback<FocusOutEvent>(_ => InspectEnded?.Invoke());
                    parent.Add(card.Button);
                    cards.Add(card);
                }
                var current = cards[i];
                current.Model = models[i];
                current.Title.text = current.Model.Title;
                current.Description.text = current.Model.Description;
                current.Status.text = current.Model.Status;
                current.Button.tooltip = current.Model.Description;
                current.Button.SetEnabled(current.Model.Enabled);
                current.Button.EnableInClassList("is-disabled", !current.Model.Enabled);
                if (!current.HasIcon || !ReferenceEquals(current.IconSource, current.Model.IconSource))
                {
                    current.HasIcon = true;
                    current.IconSource = current.Model.IconSource;
                    current.Icon.style.backgroundImage = new StyleBackground(DataIconService.GetIcon(current.IconSource));
                }
            }
        }

        public void Detail(ToolkitCardModel model)
        {
            if (model == null) return;
            Text("detail-title", model.Title);
            Text("detail-description", model.Description);
            var icon = Root.Q("detail-icon");
            if (icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(DataIconService.GetIcon(model.IconSource));
            }
        }
    }
}
