using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

// Binds the semantic elements to models, all sizing, colors and layout live in the UXML and USS
public class ToolkitGameView
{
    public UnityEvent<ToolkitCardModel> OnInspect = new UnityEvent<ToolkitCardModel>();
    public UnityEvent OnInspectEnded = new UnityEvent();
    public UnityEvent<ToolkitCardModel> OnInspectRequested = new UnityEvent<ToolkitCardModel>();
    public UnityEvent<ToolkitCardModel> OnCardActivated = new UnityEvent<ToolkitCardModel>();
    ToolkitLayoutState _layout;
    bool _hasLayout;
    public ToolkitLayoutState layout => _layout;
    // The pointer last used on the interface, unknown until one is: the device stands for it until then
    bool? _touchInput;
    public bool isPhoneLayout => _layout.isPhone;
    // A touch (or pen) has no hover: what a mouse shows on hover, a touch opens on press
    public bool isTouchInput => _touchInput ?? _layout.isTouchDevice;
    // Raised once per real change of the layout, after the root classes match it
    public event Action<ToolkitLayoutState> OnLayoutChanged;
    public IToolkitRosterDrag rosterDrag;
    public Rect inspectAnchor;
    public Func<Vector2, Vector2> screenPointProvider;
    public Func<int, bool> canBeginPointer;
    public event Action OnGesturesCancelled;
    public event Action OnClosePopover;
    public void CancelGestures() { OnGesturesCancelled?.Invoke(); rosterDrag?.Cancel(); }
    public void ClosePopover() { OnClosePopover?.Invoke(); }

    // Structural elements must not consume pointer events over the empty battlefield
    static readonly string[] structuralElements =
    {
        "hud-root",
        "safe-frame",
        "main-content",
        "world-space",
        "game-ui",
        "hud",
        "field-toolbar",
    };
    VisualTreeAsset _cardTemplate = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
    readonly Dictionary<string, ToolkitCardCollection> _lists = new Dictionary<string, ToolkitCardCollection>();
    readonly List<KeyValuePair<Button, Action>> _clicks = new List<KeyValuePair<Button, Action>>();
    bool _released;
    readonly DataIconService _icons = new DataIconService();
    public DataIconService icons
    {
        get { return _icons; }
    }

    IToolkitIconProvider _iconProvider;
    ToolkitCardModel _detail;
    VisualElement _root;
    public VisualElement root
    {
        get { return _root; }
    }

    public ToolkitGameView(VisualElement root, IToolkitIconProvider iconProvider = null)
    {
        _root = root;
        _root.RegisterCallback<PointerDownEvent>(GatePointer, TrickleDown.TrickleDown);
        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        _root.pickingMode = PickingMode.Ignore;
        _root.AddToClassList("toolkit-theme");
        ToolkitTemplates.PreparePicking(_root);
        foreach (string name in structuralElements)
        {
            VisualElement element = _root.Q(name);
            if (element != null)
            {
                element.pickingMode = PickingMode.Ignore;
            }
        }

        SetIconProvider(iconProvider);
    }

    void GatePointer(PointerDownEvent evt)
    {
        if (!CanBeginPointer(evt.pointerId))
        {
            evt.StopImmediatePropagation();
            return;
        }

        RecordPointer(evt.pointerType);
    }

    // A touch counts on its press, a mouse as soon as it moves. The legacy mouse copy of a touch is refused by the
    // same gate as its press, so it never passes for a mouse
    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!ToolkitInfoTrigger.IsTouch(evt.pointerType) && evt.deltaPosition != Vector3.zero
            && CanBeginPointer(evt.pointerId))
        {
            RecordPointer(evt.pointerType);
        }
    }

    bool CanBeginPointer(int pointerId)
    {
        return canBeginPointer?.Invoke(pointerId) ?? true;
    }

    public void RecordPointer(string pointerType)
    {
        _touchInput = ToolkitInfoTrigger.IsTouch(pointerType);
    }

    public void SetIconProvider(IToolkitIconProvider provider)
    {
        if (_released || ReferenceEquals(_iconProvider, provider))
        {
            return;
        }

        if (_iconProvider != null)
        {
            _iconProvider.Changed -= RefreshIcons;
        }

        _iconProvider = provider;
        if (_iconProvider != null)
        {
            _iconProvider.Changed += RefreshIcons;
        }

        RefreshIcons();
    }

    public Texture2D GetIcon(object source, out bool isPortrait)
    {
        isPortrait = false;
        if (_released)
        {
            return null;
        }

        Entity entity = source as Entity;
        EntityData data = entity != null ? entity.data : source as EntityData;
        Texture2D portrait =
            data != null && _iconProvider != null
                ? _iconProvider.GetCreatureIcon(data, entity != null ? entity.entityType : Entity.EntityType.Player)
                : null;
        isPortrait = portrait != null;
        Texture2D supplied = isPortrait ? portrait : (_iconProvider as IToolkitDataIconProvider)?.GetDataIcon(source);
        return supplied ? supplied : _icons.GetIcon(data != null ? data : source);
    }

    // An invalidation also updates a hovered detail or a drawer whose model did not change.
    public void RefreshIcons()
    {
        if (_released)
        {
            return;
        }

        foreach (ToolkitCardCollection cards in _lists.Values)
        {
            cards.RefreshIcons();
        }

        if (_detail != null)
        {
            ShowDetail(_detail);
        }
    }

    public void SetText(string name, string value)
    {
        if (_released)
        {
            return;
        }

        Label label = _root.Q<Label>(name);
        if (label != null && label.text != value)
        {
            label.text = value;
        }
    }

    public void Show(string name, bool show)
    {
        if (_released)
        {
            return;
        }

        VisualElement element = _root.Q(name);
        if (element != null)
        {
            element.EnableInClassList("is-hidden", !show);
        }
    }

    public void SetButton(string name, string title, bool isEnabled)
    {
        if (_released)
        {
            return;
        }

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
        if (_released)
        {
            return;
        }

        Button button = _root.Q<Button>(name);
        if (button != null)
        {
            button.clicked += action;
            _clicks.Add(new KeyValuePair<Button, Action>(button, action));
        }
    }

    public void RemoveClickListener(string name, Action action)
    {
        for (int i = _clicks.Count - 1; i >= 0; i--)
        {
            KeyValuePair<Button, Action> click = _clicks[i];
            if (click.Key.name == name && click.Value == action)
            {
                click.Key.clicked -= click.Value;
                _clicks.RemoveAt(i);
            }
        }
    }

    public void SetResource(string name, float value, float maximum)
    {
        if (_released)
        {
            return;
        }

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

        if (_released)
        {
            return;
        }

        if (!_lists.TryGetValue(name, out ToolkitCardCollection cards))
        {
            cards = new ToolkitCardCollection(this, parent, _cardTemplate);
            _lists.Add(name, cards);
        }

        cards.Refresh(models);
    }

    public void ShowDetail(ToolkitCardModel model)
    {
        if (_released || model == null)
        {
            return;
        }

        _detail = model;
        Show("detail-attributes", model.source is Entity);
        if (!(model.source is Entity))
        {
            _root.Query<VisualElement>(className: "detail-section").ForEach(HideDetailSection);
        }

        SetText("detail-title", model.title);
        // The inspected card has room to explain the statuses its text cites
        SetText("detail-description", ToolkitStatusText.WithExplanation(model.description));
        VisualElement icon = _root.Q("detail-icon");
        if (icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(GetIcon(model.iconSource, out bool isPortrait));
            icon.EnableInClassList("creature-portrait", isPortrait);
        }
    }

    static void HideDetailSection(VisualElement section)
    {
        section.AddToClassList("is-hidden");
    }

    // Destroys the icons this view generated
    // Applied every frame: only a state that differs from the last one is announced
    public bool SetLayout(ToolkitLayoutState state)
    {
        if (_hasLayout && _layout.Equals(state))
        {
            return false;
        }

        // A rotation or a resize under a finger: the gesture was measured in the previous layout, it is dropped
        if (_hasLayout)
        {
            CancelGestures();
        }

        _hasLayout = true;
        _layout = state;
        OnLayoutChanged?.Invoke(state);
        return true;
    }

    public void Release()
    {
        OnLayoutChanged = null;
        CancelGestures();
        _root.UnregisterCallback<PointerDownEvent>(GatePointer, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        if (_iconProvider != null)
        {
            _iconProvider.Changed -= RefreshIcons;
        }

        _iconProvider = null;
        foreach (ToolkitCardCollection cards in _lists.Values)
        {
            cards.Dispose();
        }

        foreach (KeyValuePair<Button, Action> click in _clicks)
        {
            click.Key.clicked -= click.Value;
        }

        _clicks.Clear();
        _lists.Clear();
        OnInspect.RemoveAllListeners();
        OnInspectEnded.RemoveAllListeners();
        OnInspectRequested.RemoveAllListeners();
        OnCardActivated.RemoveAllListeners();
        _released = true;
        _detail = null;
        _icons.Clear();
    }
}
