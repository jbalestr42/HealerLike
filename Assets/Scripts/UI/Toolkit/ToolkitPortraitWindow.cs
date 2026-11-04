using System;
using UnityEngine;

// Pause menu button turning the desktop window into a phone-shaped portrait one, to see the mobile layout on PC, and
// back. The window is the process's, so the size to come back to outlives the scene that switched it.
public class ToolkitPortraitWindow : IDisposable
{
    public const string ButtonName = "portrait-button";
    // A tall phone screen (390 x 844 points)
    public const float PhoneAspect = 390f / 844f;
    // Room left on the display for the taskbar and the window title bar
    public const float DisplayHeightShare = 0.9f;

    public struct WindowState
    {
        public int width;
        public int height;
        public FullScreenMode mode;

        public WindowState(int width, int height, FullScreenMode mode)
        {
            this.width = width;
            this.height = height;
            this.mode = mode;
        }
    }

    static WindowState? s_restore;

    ToolkitGameView _view;
    Func<Vector2Int> _displaySize;
    Func<WindowState> _currentWindow;
    Action<WindowState> _applyWindow;

    public static bool isPortrait => s_restore.HasValue;

    // Only a desktop player owns its window: the Editor's Game view and a phone ignore Screen.SetResolution
    public static bool IsAvailable(RuntimePlatform platform)
    {
        return platform == RuntimePlatform.WindowsPlayer
            || platform == RuntimePlatform.OSXPlayer
            || platform == RuntimePlatform.LinuxPlayer;
    }

    // As tall as the display allows, at a phone's aspect, never wider than the display
    public static Vector2Int GetPortraitSize(Vector2Int displaySize)
    {
        int height = Mathf.Max(1, Mathf.RoundToInt(displaySize.y * DisplayHeightShare));
        int width = Mathf.RoundToInt(height * PhoneAspect);
        if (width > displaySize.x)
        {
            width = displaySize.x;
            height = Mathf.RoundToInt(width / PhoneAspect);
        }

        return new Vector2Int(Mathf.Max(1, width), height);
    }

    public static string GetLabel(bool portrait)
    {
        return portrait ? "Desktop view" : "Phone view";
    }

    public void Init(
        ToolkitGameView view,
        bool isAvailable,
        Func<Vector2Int> displaySize = null,
        Func<WindowState> currentWindow = null,
        Action<WindowState> applyWindow = null
    )
    {
        Dispose();
        _view = view;
        _displaySize = displaySize ?? (() => new Vector2Int(Display.main.systemWidth, Display.main.systemHeight));
        _currentWindow = currentWindow ?? (() => new WindowState(Screen.width, Screen.height, Screen.fullScreenMode));
        _applyWindow = applyWindow ?? (state => Screen.SetResolution(state.width, state.height, state.mode));
        view.Show(ButtonName, isAvailable);
        view.AddClickListener(ButtonName, Toggle);
        RefreshLabel();
    }

    public void Toggle()
    {
        if (s_restore.HasValue)
        {
            WindowState restore = s_restore.Value;
            s_restore = null;
            _applyWindow(restore);
        }
        else
        {
            s_restore = _currentWindow();
            Vector2Int size = GetPortraitSize(_displaySize());
            _applyWindow(new WindowState(size.x, size.y, FullScreenMode.Windowed));
        }

        RefreshLabel();
    }

    void RefreshLabel()
    {
        _view?.SetButton(ButtonName, GetLabel(isPortrait), true);
    }

    #region IDisposable
    public void Dispose()
    {
        if (_view != null)
        {
            _view.RemoveClickListener(ButtonName, Toggle);
        }

        _view = null;
    }
    #endregion
}
