using System.Globalization;

// Pause and slow motion of the sandbox, the panel applies timeScale to Time.timeScale
public class SandboxTimeControl
{
    static readonly float[] Speeds = { 1f, 0.5f, 0.25f };

    int _speedIndex = 0;

    bool _isPaused = false;
    public bool isPaused { get { return _isPaused; } }

    public float speed => Speeds[_speedIndex];
    // The speed is kept while paused, to resume at the same pace
    public float timeScale => _isPaused ? 0f : speed;

    public string pauseLabel => _isPaused ? "Resume" : "Pause";
    public string speedLabel => "Speed x" + speed.ToString(CultureInfo.InvariantCulture);

    public void TogglePause()
    {
        _isPaused = !_isPaused;
    }

    // Slower and slower, then back to the normal speed
    public void NextSpeed()
    {
        _speedIndex = (_speedIndex + 1) % Speeds.Length;
    }
}
