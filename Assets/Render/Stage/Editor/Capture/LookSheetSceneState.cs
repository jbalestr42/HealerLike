using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Look;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class LookSheetSceneState : IDisposable
    {
        readonly List<Object> _created = new List<Object>();
        RenderManager _manager;
        BattleFocus _focus;
        bool _focusEnabled;
        Pose _cameraPose;
        float _aspect;
        LookSettings _look;

        public List<Object> created { get { return _created; } }
        public Camera flatCamera { get; private set; }

        public void Init(RenderManager manager)
        {
            _manager = manager;
            _focus = Object.FindAnyObjectByType<BattleFocus>();
            if (_focus != null)
            {
                _focusEnabled = _focus.enabled;
                _focus.enabled = false;
            }
            Transform camera = manager.gameCamera.transform;
            _cameraPose = new Pose(camera.position, camera.rotation);
            _aspect = manager.gameCamera.aspect;
            _look = manager.look.settings;
            GameObject flatGo = new GameObject("LookSheetFlatCamera");
            _created.Add(flatGo);
            flatCamera = flatGo.AddComponent<Camera>();
            flatCamera.enabled = false;
        }

        public void Dispose()
        {
            foreach (Object item in _created)
            {
                RenderObjects.Release(item);
            }
            _created.Clear();
            if (_focus != null)
            {
                _focus.enabled = _focusEnabled;
            }
            if (_manager != null && _manager.gameCamera != null)
            {
                _manager.gameCamera.transform.SetPositionAndRotation(_cameraPose.position, _cameraPose.rotation);
                _manager.gameCamera.aspect = _aspect;
                _manager.look.settings = _look;
            }
            _manager = null;
            _focus = null;
            flatCamera = null;
        }
    }
}
