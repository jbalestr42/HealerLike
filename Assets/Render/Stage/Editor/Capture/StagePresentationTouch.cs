using System;
using System.Collections;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A held finger, using the existing capture input consumed by the real EventSystem and touch Update.
    public sealed class StagePresentationTouch : IDisposable
    {
        readonly StageTouchInput _touch;
        readonly StageCaptureInput _input;

        public StagePresentationTouch(StageInterfaceActions actions)
        {
            _touch = actions.touch;
            _input = actions.ui.GetComponent<StageCaptureInput>();
            if (_touch == null || _input == null) throw new InvalidOperationException("Held capture needs configured legacy input");
        }

        public IEnumerator Frame(TouchPhase phase, Vector2 position)
        {
            _input.samples = new[] { new Touch { fingerId = 0, phase = phase, position = position, tapCount = 1 } };
            _touch.captureTouches = _input.samples;
            yield return null;
        }

        public void Dispose()
        {
            if (_touch != null) _touch.captureTouches = null;
            if (_input != null) _input.samples = Array.Empty<Touch>();
        }
    }
}
