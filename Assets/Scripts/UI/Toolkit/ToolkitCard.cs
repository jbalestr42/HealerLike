using UnityEngine;
using UnityEngine.UIElements;

// One data card of a ToolkitGameView list, reused while the list refreshes
public class ToolkitCard : System.IDisposable
{
    ToolkitGameView _view;
    ToolkitCardGesture _gesture;
    ToolkitCooldown _cooldown;
    Label _cost;
    VisualElement _health;
    VisualElement _healthFill;
    bool _compact;

    public void SetCompact()
    {
        _compact = true;
        _gesture = new ToolkitCardGesture(this, _view, OnClicked);
        _cooldown = new ToolkitCooldown();
        _icon.Add(_cooldown);
        _cost = new Label { pickingMode = PickingMode.Ignore };
        _cost.AddToClassList("spell-cost");
        _icon.Add(_cost);
        _health = new VisualElement { pickingMode = PickingMode.Ignore };
        _health.AddToClassList("creature-health");
        _healthFill = new VisualElement { pickingMode = PickingMode.Ignore };
        _healthFill.AddToClassList("creature-health-fill");
        _health.Add(_healthFill);
        _icon.Add(_health);
    }
    Button _button;
    Button _info;
    VisualElement _root;
    public VisualElement root
    {
        get { return _root; }
    }

    VisualElement _icon;
    Label _title;
    Label _description;
    Label _status;
    object _iconSource;
    ToolkitCardModel _model;
    public ToolkitCardModel model
    {
        get { return _model; }
    }

    public Button button
    {
        get { return _button; }
    }

    public void Init(ToolkitGameView view, VisualTreeAsset cardTemplate)
    {
        Dispose();
        if (
            !ToolkitTemplates.TryClone(cardTemplate, "card-shell", out _root)
            || !ToolkitTemplates.Require(_root, "data-card", out _button)
            || !ToolkitTemplates.Require(_root, "card-info", out _info)
            || !ToolkitTemplates.Require(_root, "card-icon", out _icon)
            || !ToolkitTemplates.Require(_root, "card-title", out _title)
            || !ToolkitTemplates.Require(_root, "card-description", out _description)
            || !ToolkitTemplates.Require(_root, "card-status", out _status)
        )
        {
            Dispose();
            return;
        }

        _view = view;
        _info.clicked += OnInfoClicked;
        _button.clicked += OnClicked;
        _button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _button.RegisterCallback<FocusInEvent>(OnFocusIn);
        _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        _button.RegisterCallback<FocusOutEvent>(OnFocusOut);
    }

    public void ShowInfo(bool show)
    {
        _info.EnableInClassList("is-hidden", !show);
    }

    public void Refresh(ToolkitCardModel model)
    {
        _model = model;
        _button.userData = model;
        _title.text = _model.title;
        _description.text = _model.description;
        _status.text = _model.status;
        _button.tooltip = _model.description;
        _info.tooltip = "Inspect " + _model.title;
        _button.SetEnabled(_compact || _model.isEnabled);
        _button.EnableInClassList("is-disabled", !_compact && !_model.isEnabled);
        if (_compact)
        {
            bool spell = model.source is CharacterSkillSlot;
            _cost.text = spell ? model.spell.cost.ToString("0") : "";
            _cooldown.remaining = spell ? model.spell.remaining : 0;
            _button.EnableInClassList("mana-shortage", spell && model.spell.insufficientMana);
            _health.style.display = model.healthFraction >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _healthFill.style.width = Length.Percent(Mathf.Clamp01(model.healthFraction) * 100);
        }
        // Source side/data can change while the Entity reference stays the same. The provider
        // caches the screenshot, so requesting it again never renders an unchanged creature.
        _iconSource = _model.iconSource;
        RefreshIcon();
    }

    public void RefreshIcon()
    {
        if (_view == null || _model == null)
        {
            return;
        }

        _icon.style.backgroundImage = new StyleBackground(_view.GetIcon(_iconSource, out bool isPortrait));
        _icon.EnableInClassList("creature-portrait", isPortrait);
        _button.EnableInClassList("creature-card", isPortrait);
    }

    public void Dispose()
    {
        _gesture?.Dispose();
        _gesture = null;
        if (_button != null)
        {
            _button.clicked -= OnClicked;
            _button.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            _button.UnregisterCallback<FocusInEvent>(OnFocusIn);
            _button.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _button.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        if (_info != null)
        {
            _info.clicked -= OnInfoClicked;
        }

        if (_root != null)
        {
            _root.RemoveFromHierarchy();
        }

        _root = null;
        _button = null;
        _info = null;
        _view = null;
        _model = null;
        _iconSource = null;
    }

    void OnClicked()
    {
        if (_model != null && _model.isEnabled && _model.activate != null)
        {
            ToolkitCardModel activated = _model;
            ToolkitGameView view = _view;
            activated.activate.Invoke(activated);
            if (_view == view && _model == activated)
            {
                view.OnCardActivated.Invoke(activated);
            }
        }
    }

    void OnInfoClicked()
    {
        _view.OnInspectRequested.Invoke(_model);
    }

    void OnPointerEnter(PointerEnterEvent evt)
    {
        if (!_compact && !_view.isTouchLayout)
        {
            _view.OnInspect.Invoke(_model);
        }
    }

    void OnFocusIn(FocusInEvent evt)
    {
        if (!_compact && !_view.isTouchLayout)
        {
            _view.OnInspect.Invoke(_model);
        }
    }

    void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (!_compact && !_view.isTouchLayout)
        {
            _view.OnInspectEnded.Invoke();
        }
    }

    void OnFocusOut(FocusOutEvent evt)
    {
        if (!_compact && !_view.isTouchLayout)
        {
            _view.OnInspectEnded.Invoke();
        }
    }
}
