using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The studio clock over one preview length: play, pause, loop, speed and a scrub bar
    public class StudioTimeline
    {
        // A slow editor frame advances the clock by this much at most
        static readonly double maxStep = 0.1;

        float _time;
        float _speed = 1f;
        bool _isPlaying = true;
        bool _isLooping = true;
        double _lastTick;

        public float time { get { return _time; } set { _time = value; } }

        public bool isPlaying { get { return _isPlaying; } }

        public void Init()
        {
            _lastTick = EditorApplication.timeSinceStartup;
        }

        // Advances by real time while playing a subject; true when the window should repaint
        public bool Tick(float duration, bool hasSubject)
        {
            double now = EditorApplication.timeSinceStartup;
            bool isAdvanced = _isPlaying && hasSubject;
            if (isAdvanced)
            {
                _time += (float)System.Math.Min(now - _lastTick, maxStep) * _speed;
                if (_time >= duration)
                {
                    if (_isLooping)
                    {
                        _time %= duration;
                    }
                    else
                    {
                        _time = duration;
                        _isPlaying = false;
                    }
                }
            }

            _lastTick = now;
            return isAdvanced;
        }

        // Play from the start once a stopped clock has reached the end
        public void Toggle(float duration)
        {
            if (!_isPlaying && _time >= duration)
            {
                _time = 0f;
            }

            _isPlaying = !_isPlaying;
            _lastTick = EditorApplication.timeSinceStartup;
        }

        // The transport row and the scrub bar; true when Restart was pressed
        public bool Draw(StudioStyles styles, float duration)
        {
            bool isRestarted = false;
            EditorGUILayout.BeginHorizontal();
            string playLabel = "Play";
            if (_isPlaying)
            {
                playLabel = "Pause";
            }

            if (GUILayout.Button(playLabel, GUILayout.Width(65f)))
            {
                Toggle(duration);
            }

            if (GUILayout.Button("Restart", GUILayout.Width(65f)))
            {
                _time = 0f;
                isRestarted = true;
            }

            _isLooping = GUILayout.Toggle(_isLooping, "Loop", GUILayout.Width(50f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Speed", styles.small, GUILayout.Width(36f));
            _speed = EditorGUILayout.Slider(_speed, 0.1f, 3f, GUILayout.MinWidth(75f));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _time = EditorGUILayout.Slider(_time, 0f, duration);
            if (EditorGUI.EndChangeCheck())
            {
                _isPlaying = false;
            }
            return isRestarted;
        }

        public string Readout(float duration)
        {
            return _time.ToString("0.00") + " / " + duration.ToString("0.00") + " s  ·  Space: play  ·  F: frame";
        }
    }
}
