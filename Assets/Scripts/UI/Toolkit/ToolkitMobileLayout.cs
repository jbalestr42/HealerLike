using System;
using UnityEngine;
using UnityEngine.UIElements;

// Drawers cover the board temporarily, keeping the camera's usable field stable.
public class ToolkitMobileLayout
{
    ToolkitGameView _view;
    ToolkitGameContext _context;
    UIDocument _document;
    VisualElement _hud;
    VisualElement _speed;
    VisualElement _markers;
    bool _isMobile;
    bool _layoutApplied;
    bool _partyOpen;
    bool _detailOpen;
    bool _hadInteraction;
    Vector2Int _screen;
    Rect _safeArea;
    Action _toggleFocus;
    bool _focused;

    public Rect normalizedWorldViewport
    {
        get
        {
            if (_view == null || _view.root.panel == null)
            {
                return new Rect(0.04f, 0.24f, 0.92f, 0.64f);
            }

            return ToolkitScreenLayout.GetViewport(_view.root.Q("world-space").worldBound, _view.root.worldBound);
        }
    }

    public void Init(ToolkitGameView view, ToolkitGameContext context, UIDocument document)
    {
        _view = view;
        _context = context;
        _document = document;
        _hud = view.root.Q("hud-root");
        _speed = view.root.Q("speed-controls");
        _markers = view.root.Q("mark-entity-toggle");
        view.AddClickListener("party-button", ToggleParty);
        view.AddClickListener("detail-button", ToggleDetail);
        view.AddClickListener("party-close-button", OnCloseDrawers);
        view.AddClickListener("detail-close-button", OnCloseDrawers);
        view.AddClickListener("cancel-button", CancelInteraction);
        view.AddClickListener("focus-button", ToggleFocus);
        view.OnInspectRequested.AddListener(Inspect);
        view.OnCardActivated.AddListener(OnCardActivated);
        view.root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        view.root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        _layoutApplied = false;
        _screen = Vector2Int.zero;
        Update();
        RefreshFocus();
    }

    public static PanelSettings CreatePanelSettings(PanelSettings source)
    {
        PanelSettings settings = source != null ? UnityEngine.Object.Instantiate(source)
            : ScriptableObject.CreateInstance<PanelSettings>();
        settings.name = "Toolkit runtime panel";
        settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Toolkit/RuntimeTheme");
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        return settings;
    }

    public void Update()
    {
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (_screen == screen && _safeArea == Screen.safeArea)
        {
            return;
        }

        _screen = screen;
        _safeArea = Screen.safeArea;
        float scale = ToolkitScreenLayout.GetScale(screen.x, screen.y, Application.isMobilePlatform);
        _document.panelSettings.scale = scale;
        Rect safe = ToolkitScreenLayout.GetSafePanelRect(screen.x, screen.y, _safeArea, scale);
        float right = screen.x / scale - safe.xMax;
        float bottom = screen.y / scale - safe.yMax;
        _hud.style.paddingLeft = safe.xMin + 8f;
        _hud.style.paddingTop = safe.yMin + 8f;
        _hud.style.paddingRight = right + 8f;
        _hud.style.paddingBottom = bottom + 8f;
        foreach (VisualElement modal in _view.root.Query(className: "modal-layer").ToList())
        {
            modal.style.left = safe.xMin + 8f;
            modal.style.top = safe.yMin + 8f;
            modal.style.right = right + 8f;
            modal.style.bottom = bottom + 8f;
        }

        Resize(screen.x / scale, screen.y / scale);
    }

    public void Refresh()
    {
        bool blocked = IsBlocked();
        bool interacting = _context.hasInteraction;
        if (blocked || (interacting && !_hadInteraction))
        {
            CloseDrawers();
        }

        _hadInteraction = interacting;
        _view.Show("cancel-button", interacting && !blocked);
        _view.Show("field-toolbar", !_context.isMenu);
        _view.SetButton("party-button", null, !blocked);
        _view.SetButton("detail-button", null, !blocked);
        _view.SetButton("focus-button", null, !blocked);
    }

    public void SetBattleFocus(bool focused, Action toggle)
    {
        _focused = focused;
        _toggleFocus = toggle;
        RefreshFocus();
    }

    public bool CloseDrawers()
    {
        bool wasOpen = _partyOpen || _detailOpen;
        _partyOpen = false;
        _detailOpen = false;
        if (_hud != null)
        {
            _hud.RemoveFromClassList("party-open");
            _hud.RemoveFromClassList("detail-open");
        }

        if (wasOpen)
        {
            _view.OnInspectEnded.Invoke();
        }

        return wasOpen;
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        Resize(evt.newRect.width, evt.newRect.height);
    }

    public void Resize(float width, float height)
    {
        bool mobile = width < 760f || height < 500f;
        _hud.EnableInClassList("is-compact", width < 1100f);
        _hud.EnableInClassList("is-mobile", mobile);
        _hud.EnableInClassList("is-landscape", width > height);
        _view.isTouchLayout = mobile;
        if (_layoutApplied && _isMobile == mobile)
        {
            return;
        }

        _layoutApplied = true;
        _isMobile = mobile;
        VisualElement controls = _view.root.Q(mobile ? "pause-settings" : "command-section");
        controls.Add(_speed);
        controls.Add(_markers);
        _view.SetButton("inventory-button", mobile ? "Bag" : "Inventory", true);
        CloseDrawers();
    }

    void OnCloseDrawers()
    {
        CloseDrawers();
    }

    void ToggleParty()
    {
        bool open = !_partyOpen;
        CloseDrawers();
        _partyOpen = open;
        _hud.EnableInClassList("party-open", open);
    }

    void ToggleDetail()
    {
        bool open = !_detailOpen;
        CloseDrawers();
        _detailOpen = open;
        _hud.EnableInClassList("detail-open", open);
    }

    void Inspect(ToolkitCardModel model)
    {
        if (_isMobile)
        {
            CloseDrawers();
            _detailOpen = true;
            _hud.AddToClassList("detail-open");
        }

        _view.OnInspect.Invoke(model);
    }

    void OnCardActivated(ToolkitCardModel model)
    {
        if (!_isMobile)
        {
            return;
        }

        CloseDrawers();
        if (model.source is Entity)
        {
            _detailOpen = true;
            _hud.AddToClassList("detail-open");
        }
    }

    void CancelInteraction()
    {
        if (_context.interaction != null)
        {
            _context.interaction.CancelInteraction();
        }

        Refresh();
    }

    void ToggleFocus()
    {
        if (_toggleFocus != null)
        {
            _toggleFocus.Invoke();
        }
    }

    bool IsBlocked()
    {
        return _context.isMenu || _context.isPaused || _context.isInventoryOpen
            || (_context.ui != null && !_context.IsCurrentView(ViewType.Game));
    }

    void RefreshFocus()
    {
        if (_view != null)
        {
            _view.Show("focus-button", _toggleFocus != null);
            _view.SetButton("focus-button", _focused ? "Overview" : "Focus battle", !IsBlocked());
        }
    }
}
