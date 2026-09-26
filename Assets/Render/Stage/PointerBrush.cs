using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A finger or the mouse drawn through the grass parts it. A press that starts on the interface or on a
    // creature is someone else's gesture and brushes nothing; one that starts on open ground brushes the grass
    // along the pointer's path on the board plane for as long as it is held.
    public class PointerBrush : MonoBehaviour, IZoneBody
    {
        // In cells: how wide a finger brushes, and how hard it presses
        public static readonly float BrushRadius = 0.18f;
        public static readonly float BrushPress = 0.35f;

        ZoneRegistry _zones;
        Camera _camera;
        StageTouchInput _touch;
        float _groundY;
        float _cellSize = 1f;
        Vector3 _previous;
        Vector3 _current;
        bool _hasPoint;
        // The finger the stroke follows, -1 for the mouse or none
        int _finger = -1;

        bool _isBrushing;
        public bool isBrushing { get { return _isBrushing; } }

        public void Init(ZoneRegistry zones, Camera camera, float groundY, float cellSize)
        {
            if (_zones != null)
            {
                _zones.RemoveBody(this);
            }

            _zones = zones;
            _camera = camera;
            _groundY = groundY;
            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            _isBrushing = false;
            _hasPoint = false;
            if (_zones != null)
            {
                _zones.AddBody(this);
            }
        }

        void Update()
        {
            bool isDown;
            bool hasBegun;
            Vector2 screen;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Touching(touch.fingerId, touch.phase, touch.position);
                return;
            }

            _finger = -1;
            isDown = Input.GetMouseButton(0);
            hasBegun = Input.GetMouseButtonDown(0);
            screen = Input.mousePosition;
            Point(isDown, hasBegun, screen);
        }

        // One frame of the first touch. The stroke follows the finger that started it: when it lifts and another
        // finger becomes the first touch, that finger brushes nothing until it begins a stroke of its own.
        public void Touching(int fingerId, TouchPhase phase, Vector2 screen)
        {
            bool hasBegun = phase == TouchPhase.Began;
            if (hasBegun)
            {
                _finger = fingerId;
            }

            bool isDown = fingerId == _finger && phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
            Point(isDown, hasBegun, screen);
        }

        // One frame of the pointer: where it is on screen, whether it is held, whether it was pressed just now
        public void Point(bool isDown, bool hasBegun, Vector2 screen)
        {
            if (!isDown || _camera == null)
            {
                _isBrushing = false;
                _hasPoint = false;
                return;
            }

            if (hasBegun)
            {
                _isBrushing = IsOpenGround(screen);
                _hasPoint = false;
            }

            if (!_isBrushing || !Ground(_camera.ScreenPointToRay(screen), _groundY, out Vector3 point))
            {
                return;
            }

            _previous = _hasPoint ? _current : point;
            _current = point;
            _hasPoint = true;
        }

        bool IsOpenGround(Vector2 screen)
        {
            if (_touch == null)
            {
                _touch = FindAnyObjectByType<StageTouchInput>();
            }

            if (_touch != null && _touch.IsOverInterface(screen))
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponentInParent<Entity>() != null)
            {
                return false;
            }

            return true;
        }

        // Where a ray meets the board plane, if it comes down onto it
        public static bool Ground(Ray ray, float groundY, out Vector3 point)
        {
            point = Vector3.zero;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            if (ray.direction.y >= 0f || !plane.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return RenderMath.IsFinite(point);
        }

        // The stretch the pointer brushed since last frame, low over the grass
        public int AppendCapsules(BodyCapsule[] into, int start)
        {
            if (into == null || start >= into.Length || !_isBrushing || !_hasPoint)
            {
                return 0;
            }

            Vector3 lift = Vector3.up * (BrushRadius * _cellSize);
            into[start] = new BodyCapsule
            {
                start = _previous + lift, end = _current + lift, radius = BrushRadius * _cellSize, press = BrushPress
            };
            return 1;
        }

        void OnDisable()
        {
            _isBrushing = false;
            _hasPoint = false;
        }

        void OnDestroy()
        {
            if (_zones != null)
            {
                _zones.RemoveBody(this);
            }
        }
    }
}
