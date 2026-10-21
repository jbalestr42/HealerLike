using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // A measurement temporarily changes presentation; every borrowed value returns to its exact prior value.
    public class GrassJitterState : IDisposable
    {
        readonly struct FieldState
        {
            public readonly GrassField field;
            public readonly GrassDraw draw;
            public readonly float wind;
            public readonly ShadowCastingMode shadows;

            public FieldState(GrassField field)
            {
                this.field = field;
                draw = field.tuftDraw;
                wind = field.windStrength;
                shadows = draw != null ? draw.shadowCastingMode : ShadowCastingMode.Off;
            }
        }

        readonly Camera _camera;
        readonly BattleFocus _focus;
        readonly bool _focusEnabled;
        readonly Vector3 _position;
        readonly Quaternion _rotation;
        readonly float _aspect;
        readonly float _captureDelta;
        readonly List<FieldState> _fields = new List<FieldState>();
        readonly Dictionary<EnvironmentSway, bool> _sways = new Dictionary<EnvironmentSway, bool>();
        bool _isDisposed;

        public int errorCount { get; private set; }

        public GrassJitterState(Camera camera, BattleFocus focus, IEnumerable<GrassField> fields)
        {
            _camera = camera;
            _focus = focus;
            _focusEnabled = focus != null && focus.enabled;
            _position = camera.transform.position;
            _rotation = camera.transform.rotation;
            _aspect = camera.aspect;
            _captureDelta = Time.captureDeltaTime;
            foreach (GrassField field in fields)
            {
                if (field != null)
                {
                    _fields.Add(new FieldState(field));
                }
            }
            Application.logMessageReceived += OnLog;
        }

        public void UseOverview(Pose pose, float aspect)
        {
            if (_focus != null)
            {
                _focus.enabled = false;
            }
            _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _camera.aspect = aspect;
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorCount++;
            }
        }

        public void SetWind(float strength)
        {
            foreach (FieldState state in _fields)
            {
                if (state.field != null)
                {
                    state.field.windStrength = strength;
                }
            }
        }

        public void SetShadows(ShadowCastingMode mode)
        {
            foreach (FieldState state in _fields)
            {
                if (state.draw != null)
                {
                    state.draw.shadowCastingMode = mode;
                }
            }
        }

        public void RestoreEnvironment()
        {
            foreach (FieldState state in _fields)
            {
                if (state.field != null)
                {
                    state.field.windStrength = state.wind;
                }
                if (state.draw != null)
                {
                    state.draw.shadowCastingMode = state.shadows;
                }
            }
        }

        public void StopSways(IEnumerable<EnvironmentSway> sways)
        {
            foreach (EnvironmentSway sway in sways)
            {
                if (sway != null && !_sways.ContainsKey(sway))
                {
                    _sways.Add(sway, sway.enabled);
                    sway.enabled = false;
                }
            }
        }

        public void RestoreSways()
        {
            foreach (KeyValuePair<EnvironmentSway, bool> state in _sways)
            {
                if (state.Key != null)
                {
                    state.Key.enabled = state.Value;
                }
            }
            _sways.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                Time.captureDeltaTime = _captureDelta;
                RestoreEnvironment();
                RestoreSways();
                if (_camera != null)
                {
                    _camera.transform.SetPositionAndRotation(_position, _rotation);
                    _camera.aspect = _aspect;
                }
                if (_focus != null)
                {
                    _focus.enabled = _focusEnabled;
                }
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }
    }
}
