using UnityEngine;
using UnityEngine.UIElements;

// One data card of a ToolkitGameView list, reused while the list refreshes
public class ToolkitCard
{
    ToolkitGameView _view;
    Button _button;
    VisualElement _icon;
    Label _title;
    Label _description;
    Label _status;
    object _iconSource;
    bool _hasIcon = false;

    ToolkitCardModel _model;
    public ToolkitCardModel model { get { return _model; } }

    public Button button { get { return _button; } }

    public void Init(ToolkitGameView view, VisualTreeAsset cardTemplate)
    {
        _view = view;
        TemplateContainer template = null;
        if (cardTemplate != null)
        {
            template = cardTemplate.CloneTree();
        }

        _button = Find<Button>(template, "data-card");
        _icon = Find<VisualElement>(template, "card-icon");
        _title = Find<Label>(template, "card-title");
        _description = Find<Label>(template, "card-description");
        _status = Find<Label>(template, "card-status");
        _button.AddToClassList("data-card");
        _icon.AddToClassList("data-card__icon");
        _title.AddToClassList("data-card__title");
        _description.AddToClassList("data-card__description");
        _status.AddToClassList("data-card__status");
        if (template == null)
        {
            _button.Add(_icon);
            _button.Add(_title);
            _button.Add(_description);
            _button.Add(_status);
        }

        _button.clicked += OnClicked;
        _button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _button.RegisterCallback<FocusInEvent>(OnFocusIn);
        _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        _button.RegisterCallback<FocusOutEvent>(OnFocusOut);
    }

    public void Refresh(ToolkitCardModel model)
    {
        _model = model;
        _title.text = _model.title;
        _description.text = _model.description;
        _status.text = _model.status;
        _button.tooltip = _model.description;
        _button.SetEnabled(_model.isEnabled);
        _button.EnableInClassList("is-disabled", !_model.isEnabled);
        if (!_hasIcon || !ReferenceEquals(_iconSource, _model.iconSource))
        {
            _hasIcon = true;
            _iconSource = _model.iconSource;
            _icon.style.backgroundImage = new StyleBackground(_view.icons.GetIcon(_iconSource));
        }
    }

    // A missing template or template element falls back to a plain element
    static ElementType Find<ElementType>(TemplateContainer template, string name)
                                        where ElementType : VisualElement, new()
    {
        ElementType element = null;
        if (template != null)
        {
            element = template.Q<ElementType>(name);
        }

        if (element == null)
        {
            element = new ElementType();
        }

        return element;
    }

    void OnClicked()
    {
        if (_model.isEnabled && _model.activate != null)
        {
            _model.activate.Invoke(_model);
        }
    }

    void OnPointerEnter(PointerEnterEvent evt)
    {
        _view.OnInspect.Invoke(_model);
    }

    void OnFocusIn(FocusInEvent evt)
    {
        _view.OnInspect.Invoke(_model);
    }

    void OnPointerLeave(PointerLeaveEvent evt)
    {
        _view.OnInspectEnded.Invoke();
    }

    void OnFocusOut(FocusOutEvent evt)
    {
        _view.OnInspectEnded.Invoke();
    }
}
