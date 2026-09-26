using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitSafeArea : IDisposable
{
    readonly VisualElement _root;
    readonly VisualElement _hud;
    readonly UIDocument _document;
    Vector2Int _screen;
    Rect _safeArea;
    Vector2 _size;
    float _padding;

    public ToolkitSafeArea(VisualElement root, UIDocument document)
    {
        _root = root;
        _hud = root.Q("hud-root");
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

    public Vector2 Update(Func<Rect> safeAreaProvider = null)
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
            return _size;
        }

        _screen = screen;
        _safeArea = safeArea;
        float scale = ToolkitScreenLayout.GetScale(screen.x, screen.y, Application.isMobilePlatform);
        _document.panelSettings.scale = scale;
        Rect safe = ToolkitScreenLayout.GetSafePanelRect(screen.x, screen.y, _safeArea, scale);
        float right = screen.x / scale - safe.xMax;
        float bottom = screen.y / scale - safe.yMax;
        _hud.style.paddingLeft = safe.xMin + _padding;
        _hud.style.paddingTop = safe.yMin + _padding;
        _hud.style.paddingRight = right + _padding;
        _hud.style.paddingBottom = bottom + _padding;
        foreach (VisualElement modal in _root.Query(className: "modal-layer").ToList())
        {
            modal.style.left = safe.xMin + _padding;
            modal.style.top = safe.yMin + _padding;
            modal.style.right = right + _padding;
            modal.style.bottom = bottom + _padding;
        }

        _size = new Vector2(screen.x / scale, screen.y / scale);
        return _size;
    }
}
