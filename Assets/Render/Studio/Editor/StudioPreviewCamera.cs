using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // Orbit, pan and zoom over a studio preview, and the framing that fits its subject into the view.
    // A capture saves the framing and the camera first and puts both back after, so an export at another size
    // leaves the viewport as the user left it.
    public class StudioPreviewCamera
    {
        static readonly float fieldOfView = 34f;
        static readonly float nearClip = 0.02f;
        static readonly Color backgroundColour = new Color(0.075f, 0.095f, 0.115f);

        Vector2 _defaultOrbit;
        Vector2 _orbit;
        Vector3 _target = new Vector3(0f, 0.6f, 0f);
        float _distance = 3.7f;
        float _minDistance;
        float _maxDistance;
        float _fitMargin;
        float _aspect = -1f;
        bool _isFitRequested = true;
        int _dragControl;
        string _controlName;

        // What a capture puts back
        Vector3 _savedTarget;
        float _savedDistance;
        float _savedAspect;
        bool _savedFitRequest;

        Vector3 _savedPosition;
        Quaternion _savedRotation;
        float _savedCameraAspect;
        float _savedFieldOfView;
        float _savedNear;
        float _savedFar;
        CameraClearFlags _savedClearFlags;
        Color _savedBackground;
        bool _savedHdr;

        // fitMargin grows the fitted distance so the subject keeps some air around it
        public void Init(Vector2 defaultOrbit, float minDistance, float maxDistance, float fitMargin,
            string controlName)
        {
            _defaultOrbit = defaultOrbit;
            _orbit = defaultOrbit;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _fitMargin = fitMargin;

            _controlName = controlName;
        }

        public void Reset()
        {
            _orbit = _defaultOrbit;
            _isFitRequested = true;
        }

        public void Refit()
        {
            _isFitRequested = true;
        }

        // Places the camera for a view of width by height. A new aspect refits the view unless the caller keeps
        // the user's framing, as an export does.
        public void Apply(Camera camera, float width, float height, bool isAspectRefit, IPreviewSubject subject)
        {
            camera.aspect = Mathf.Max(0.1f, width / Mathf.Max(1f, height));
            camera.fieldOfView = fieldOfView;
            if (isAspectRefit && !Mathf.Approximately(_aspect, camera.aspect))
            {
                _isFitRequested = true;
            }

            _aspect = camera.aspect;
            if (_isFitRequested)
            {
                Bounds bounds = subject.GetSubjectBounds();
                _target = bounds.center;
                float halfHeight = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
                float halfAngle = Mathf.Atan(halfHeight * Mathf.Min(1f, camera.aspect));
                _distance = Mathf.Max(1f, bounds.extents.magnitude * _fitMargin / Mathf.Sin(halfAngle));
                _isFitRequested = false;
            }

            Quaternion rotation = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
            camera.transform.SetPositionAndRotation(_target - rotation * Vector3.forward * _distance, rotation);
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = Mathf.Max(100f, _distance * 3f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColour;
            camera.allowHDR = true;
        }

        public void BeginCapture(Camera camera)
        {
            _savedTarget = _target;
            _savedDistance = _distance;
            _savedAspect = _aspect;
            _savedFitRequest = _isFitRequested;

            _savedPosition = camera.transform.position;
            _savedRotation = camera.transform.rotation;
            _savedCameraAspect = camera.aspect;
            _savedFieldOfView = camera.fieldOfView;
            _savedNear = camera.nearClipPlane;
            _savedFar = camera.farClipPlane;
            _savedClearFlags = camera.clearFlags;
            _savedBackground = camera.backgroundColor;
            _savedHdr = camera.allowHDR;
        }

        public void EndCapture(Camera camera)
        {
            _target = _savedTarget;
            _distance = _savedDistance;
            _aspect = _savedAspect;
            _isFitRequested = _savedFitRequest;

            camera.transform.SetPositionAndRotation(_savedPosition, _savedRotation);
            camera.aspect = _savedCameraAspect;
            camera.fieldOfView = _savedFieldOfView;
            camera.nearClipPlane = _savedNear;
            camera.farClipPlane = _savedFar;
            camera.clearFlags = _savedClearFlags;
            camera.backgroundColor = _savedBackground;
            camera.allowHDR = _savedHdr;
        }

        // Scroll zooms, a drag orbits, a shift-drag or middle drag pans
        public void HandleInput(Rect rect)
        {
            Event input = Event.current;
            int control = GUIUtility.GetControlID(_controlName.GetHashCode(), FocusType.Passive, rect);
            bool isDragging = _dragControl != 0 && GUIUtility.hotControl == _dragControl;
            if (input.type == EventType.ScrollWheel && rect.Contains(input.mousePosition))
            {
                _distance = Mathf.Clamp(_distance * Mathf.Exp(input.delta.y * 0.04f), _minDistance, _maxDistance);
                GUI.changed = true;
                input.Use();
            }
            else if (input.type == EventType.MouseDown && rect.Contains(input.mousePosition) && input.button <= 2)
            {
                _dragControl = control;
                GUIUtility.hotControl = control;
                input.Use();
            }
            else if (input.type == EventType.MouseDrag && isDragging)
            {
                Drag(input, rect);
                GUI.changed = true;
                input.Use();
            }
            else if (input.type == EventType.MouseUp && isDragging)
            {
                GUIUtility.hotControl = 0;
                _dragControl = 0;
                input.Use();
            }
        }

        // Lets go of the mouse if the preview goes away mid-drag
        public void Release()
        {
            if (_dragControl != 0 && GUIUtility.hotControl == _dragControl)
            {
                GUIUtility.hotControl = 0;
            }
        }

        void Drag(Event input, Rect rect)
        {
            if (input.shift || input.button == 2)
            {
                Quaternion rotation = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
                Vector3 pan = new Vector3(-input.delta.x, input.delta.y, 0f);
                _target += rotation * pan * (_distance / Mathf.Max(100f, rect.height));
                return;
            }

            _orbit.y += input.delta.x * 0.5f;
            _orbit.x = Mathf.Clamp(_orbit.x + input.delta.y * 0.5f, -80f, 85f);
        }
    }
}
