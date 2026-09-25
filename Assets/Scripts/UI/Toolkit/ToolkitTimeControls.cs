using UnityEngine;
using UnityEngine.UIElements;

// Pause and game speed buttons, through Time.timeScale
public class ToolkitTimeControls
{
    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    float _previousSpeed = 1f;

    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view)
    {
        _gameUI = gameUI;
        _context = context;
        _view = view;
        view.AddClickListener("pause-button", TogglePause);
        view.AddClickListener("resume-button", TogglePause);
        view.AddClickListener("speed-slow-button", OnSlowClicked);
        view.AddClickListener("speed-normal-button", OnNormalClicked);
        view.AddClickListener("speed-fast-button", OnFastClicked);
        RefreshSpeedChoice();
    }

    public void TogglePause()
    {
        if (_context.isMenu)
        {
            return;
        }

        _context.isPaused = !_context.isPaused;
        if (_context.isPaused)
        {
            _previousSpeed = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _previousSpeed > 0f ? _previousSpeed : 1f;
        }

        Refresh();
    }

    // Leaving the interface resumes the game at the speed it had
    public void Resume()
    {
        if (_context.isPaused)
        {
            Time.timeScale = _previousSpeed;
            _context.isPaused = false;
        }
    }

    public void ResetSpeed()
    {
        Time.timeScale = 1f;
        _previousSpeed = 1f;
        _context.isPaused = false;
        RefreshSpeedChoice();
    }

    public void SetSpeed(float speed)
    {
        _previousSpeed = speed;
        Time.timeScale = _context.isPaused ? 0f : speed;
        RefreshSpeedChoice();
        Refresh();
    }

    void Refresh()
    {
        if (_gameUI != null)
        {
            _gameUI.Refresh();
        }
    }

    void RefreshSpeedChoice()
    {
        SetSelected("speed-slow-button", Mathf.Approximately(_previousSpeed, 0.5f));
        SetSelected("speed-normal-button", Mathf.Approximately(_previousSpeed, 1f));
        SetSelected("speed-fast-button", Mathf.Approximately(_previousSpeed, 2f));
    }

    void SetSelected(string name, bool selected)
    {
        VisualElement button = _view.root.Q(name);
        if (button != null)
        {
            button.EnableInClassList("is-selected", selected);
        }
    }

    void OnSlowClicked()
    {
        SetSpeed(0.5f);
    }

    void OnNormalClicked()
    {
        SetSpeed(1f);
    }

    void OnFastClicked()
    {
        SetSpeed(2f);
    }
}
