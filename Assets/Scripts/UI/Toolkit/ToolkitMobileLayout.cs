using System;
using UnityEngine;
using UnityEngine.UIElements;

// Drawers cover the board temporarily, keeping the camera's usable field stable.
public class ToolkitMobileLayout : IDisposable
{
    ToolkitGameView _view;
    ToolkitGameContext _context;
    ToolkitSafeArea _safeArea;
    VisualElement _hud;
    bool _isMobile;
    bool _layoutApplied;
    bool _partyOpen;
    bool _detailOpen;
    bool _hadInteraction;
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
        Dispose();
        _view = view;
        _context = context;
        _safeArea = new ToolkitSafeArea(view.root, document);
        _hud = view.root.Q("hud-root");
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
        Update();
        RefreshFocus();
    }

    public static PanelSettings CreatePanelSettings(PanelSettings source)
    {
        return ToolkitTheme.CreatePanelSettings(source, null);
    }

    public void Update(Func<Rect> safeAreaProvider = null)
    {
        Vector2 size = _safeArea.Update(safeAreaProvider);
        Resize(size.x, size.y);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.RemoveClickListener("party-button", ToggleParty);
            _view.RemoveClickListener("detail-button", ToggleDetail);
            _view.RemoveClickListener("party-close-button", OnCloseDrawers);
            _view.RemoveClickListener("detail-close-button", OnCloseDrawers);
            _view.RemoveClickListener("cancel-button", CancelInteraction);
            _view.RemoveClickListener("focus-button", ToggleFocus);
            _view.OnInspectRequested.RemoveListener(Inspect);
            _view.OnCardActivated.RemoveListener(OnCardActivated);
            _view.root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        if (_safeArea != null)
        {
            _safeArea.Dispose();
            _safeArea = null;
        }

        _view = null;
        _hud = null;
        _partyOpen = false;
        _detailOpen = false;
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
        _view.SetButton(
            "pause-button",
            null,
            !_context.isMenu && (_context.ui == null || _context.IsCurrentView(ViewType.Game))
        );
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
        bool mobile = ToolkitResponsiveLayout.Apply(_view, width, height);
        if (_layoutApplied && _isMobile == mobile)
        {
            return;
        }

        _layoutApplied = true;
        _isMobile = mobile;
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
        return _context.isMenu
            || _context.isPaused
            || _context.isInventoryOpen
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
