using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitSafeArea : IDisposable
{
    readonly VisualElement _hud;
    readonly VisualElement _frame;
    readonly UIDocument _document;
    Vector2Int _screen;
    Rect _safeArea;
    ToolkitLayoutState _state;
    float _padding;

    public ToolkitSafeArea(VisualElement root, UIDocument document)
    {
        _hud = root.Q("hud-root");
        _frame = root.Q("safe-frame");
        _document = document;
        _hud.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        ReadStyle();
    }

    void ReadStyle()
    {
        _padding = ToolkitStyleValues.ReadNonNegative(_hud.customStyle, "--ui-safe-padding", 8f);
    }

    void OnStyleResolved(CustomStyleResolvedEvent evt)
    {
        ReadStyle();
        _screen = Vector2Int.zero;
    }

    public void Dispose()
    {
        _hud.UnregisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
    }

    public ToolkitLayoutState Update(Func<Rect> safeAreaProvider = null)
    {
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;
        if (safeAreaProvider != null)
        {
            Rect normalized = safeAreaProvider.Invoke();
            safeArea = new Rect(
                normalized.x * screen.x,
                normalized.y * screen.y,
                normalized.width * screen.x,
                normalized.height * screen.y
            );
        }

        if (_screen == screen && _safeArea == safeArea)
        {
            return _state;
        }

        _screen = screen;
        _safeArea = safeArea;
        _state = ToolkitLayout.Resolve(screen, safeArea, Application.isMobilePlatform);
        _document.panelSettings.scale = _state.scale;
        // One frame holds the HUD and every modal, so a modal added after this still gets the insets
        Rect safe = _state.safeRect;
        _frame.style.left = safe.xMin + _padding;
        _frame.style.top = safe.yMin + _padding;
        _frame.style.right = _state.panelSize.x - safe.xMax + _padding;
        _frame.style.bottom = _state.panelSize.y - safe.yMax + _padding;

        return _state;
    }
}
