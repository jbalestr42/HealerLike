using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // The fixture borrows the scene lighting and owns only the objects created for its capture.
    public class GroundCaptureState : IDisposable
    {
        readonly LookController[] _owners;
        readonly bool[] _enabled;
        readonly RenderPipelineAsset _pipeline;
        readonly Light _sun;
        readonly float _lookApplied;
        bool _isDisposed;
        readonly List<Object> _created = new List<Object>();

        public List<Object> created { get { return _created; } }

        public GroundCaptureState(LookController[] owners)
        {
            _owners = owners;
            _enabled = new bool[owners.Length];
            _pipeline = QualitySettings.renderPipeline;
            _sun = RenderSettings.sun;
            _lookApplied = Shader.GetGlobalFloat("_HLLookApplied");
            for (int i = 0; i < owners.Length; i++)
            {
                _enabled[i] = owners[i].enabled;
                owners[i].enabled = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            foreach (Object item in _created)
            {
                // Disable now so a delayed Destroy cannot reset the restored scene look next frame.
                if (item is GameObject gameObject)
                {
                    gameObject.SetActive(false);
                }
                RenderObjects.Release(item);
            }
            _created.Clear();
            QualitySettings.renderPipeline = _pipeline;
            RenderSettings.sun = _sun;
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i] != null)
                {
                    _owners[i].enabled = _enabled[i];
                }
            }
            Shader.SetGlobalFloat("_HLLookApplied", _lookApplied);
        }
    }
}
