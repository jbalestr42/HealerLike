using UnityEngine;

// Pause and game speed buttons, through Time.timeScale
public class ToolkitTimeControls
{
    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    float _previousSpeed = 1f;

    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view)
    {
        _gameUI = gameUI;
        _context = context;
        view.AddClickListener("pause-button", TogglePause);
        view.AddClickListener("resume-button", TogglePause);
        view.AddClickListener("speed-slow-button", OnSlowClicked);
        view.AddClickListener("speed-normal-button", OnNormalClicked);
        view.AddClickListener("speed-fast-button", OnFastClicked);
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

        _gameUI.Refresh();
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
        _context.isPaused = false;
    }

    void SetSpeed(float speed)
    {
        _context.isPaused = false;
        _previousSpeed = speed;
        Time.timeScale = speed;
        _gameUI.Refresh();
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
