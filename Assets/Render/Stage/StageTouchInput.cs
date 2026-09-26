using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Touch has no hover frame. Deliver enter and click together, without the legacy mouse copy consuming it twice.
    [DefaultExecutionOrder(-1000)]
    public class StageTouchInput : MonoBehaviour
    {
        readonly List<RaycastResult> _hits = new List<RaycastResult>();
        InteractionManager _interaction;
        bool _suspended;
        int _finger = -1;
        Vector2 _start;
        bool _blocked;
        readonly StageWorldPress _worldPress = new StageWorldPress();
        StageRosterDrag _roster;
        ToolkitGameUI _ui;
        public StageRosterDrag roster => _roster;

#if UNITY_EDITOR
        // Native capture supplies one sample per player frame; Android always reads Input.touches.
        public Touch[] captureTouches { get; set; }
#endif

        public void Init(InteractionManager interaction, GridManager grid = null)
        {
            CancelDrag();
            RestoreMouse();
            _finger = -1;
            _roster?.Dispose();
            _interaction = interaction;
            _ui = GetComponent<ToolkitGameUI>();
            _roster = interaction != null && grid != null
                ? new StageRosterDrag(interaction, grid, Raycast, IsOverInterface, ClaimRoster) : null;
            _ui?.SetRosterDrag(_roster);
        }

        void Update()
        {
            if (_interaction == null)
            {
                return;
            }

            if (_ui != null && !_ui.acceptsWorldInput) { Interrupt(); return; }

            int touchCount = Input.touchCount;
#if UNITY_EDITOR
            if (captureTouches != null)
            {
                touchCount = captureTouches.Length;
            }
#endif
            if (touchCount == 0)
            {
                bool useMouse = true;
#if UNITY_EDITOR
                useMouse = captureTouches == null;
#endif
                if (useMouse && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
                {
                    SuspendMouse();
                    ProcessTouch(99, Input.GetMouseButtonDown(0) ? TouchPhase.Began
                        : Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved, Input.mousePosition);
                    return;
                }
                if (_finger >= 0) _ui?.CancelGestures();
                CancelDrag();
                _finger = -1;
                RestoreMouse();
                return;
            }

            SuspendMouse();

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch;
#if UNITY_EDITOR
                if (captureTouches != null)
                {
                    touch = captureTouches[i];
                }
                else
#endif
                {
                    touch = Input.GetTouch(i);
                }

                if (_finger < 0 || touch.fingerId == _finger)
                {
                    ProcessTouch(touch.fingerId, touch.phase, touch.position);
                    return;
                }
            }

            CancelDrag();
            _finger = -1;
        }

        public void ProcessTouch(int finger, TouchPhase phase, Vector2 position)
        {
            if (_interaction == null || (!_interaction.enabled && !_suspended))
            {
                return;
            }

            if (phase == TouchPhase.Began && _finger < 0)
            {
                _finger = finger;
                _start = position;
                _blocked = IsOverInterface(position);
                if (!_blocked && Raycast(position, out RaycastHit hit))
                    _worldPress.Begin(finger, PanelPoint(position), hit, _interaction.GetInteraction() == null);
            }

            if (finger != _finger)
            {
                return;
            }

            if (phase == TouchPhase.Canceled || IsOverInterface(position))
            {
                CancelDrag();
                _blocked = true;
            }
            // A held finger supplies placement hover, while release remains the single owner of activation.
            // This only moves the legacy cosmetic model; the placement adapter follows it in LateUpdate.
            AInteraction placement = _interaction.GetInteraction();
            if (!_blocked && placement is EntityGridInteraction
                && (phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                && Raycast(position, out RaycastHit placementHit, placement.GetLayerMask())
                && placement.IsValidTarget(placementHit.collider.gameObject))
            {
                placement.OnMouseOver(placementHit);
            }

            if (!_blocked && Raycast(position, out RaycastHit worldHit))
            {
                _worldPress.Move(finger, PanelPoint(position), worldHit, _ui, position);
                if (phase == TouchPhase.Ended)
                {
                    bool consumed = _worldPress.consumed;
                    _worldPress.End(worldHit);
                    _blocked |= consumed;
                }
            }

            if (phase == TouchPhase.Ended)
            {
                float threshold = 18f * Mathf.Min(Screen.width, Screen.height) / 390f;
                if (!_blocked && Vector2.Distance(_start, position) <= threshold)
                {
                    Tap(position);
                }
            }

            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                CancelDrag();
                _finger = -1;
            }
        }

        protected virtual bool Raycast(Vector2 position, out RaycastHit hit, int mask = Physics.DefaultRaycastLayers)
        {
            hit = default;
            return Camera.main != null && Physics.Raycast(Camera.main.ScreenPointToRay(position),
                out hit, Mathf.Infinity, mask);
        }

        Vector2 PanelPoint(Vector2 point)
        {
            float scale = ToolkitScreenLayout.GetScale(Screen.width, Screen.height, Application.isMobilePlatform);
            return new Vector2(point.x, -point.y) / scale;
        }

        void ClaimRoster()
        {
            _blocked = true;
            _worldPress.Cancel();
        }
        void CancelDrag() { _worldPress.Cancel(); }
        void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
        void OnApplicationPause(bool paused) { if (paused) Interrupt(); }
        public void Interrupt()
        {
            _ui?.CancelGestures(); _roster?.Cancel(); CancelDrag();
            _blocked = true; _finger = -1; RestoreMouse();
        }

        void OnDisable()
        {
            _roster?.Cancel();
            _ui?.CancelGestures();
            CancelDrag();
            _finger = -1;
            RestoreMouse();
        }

        void SuspendMouse()
        {
            if (_interaction.enabled) { _interaction.enabled = false; _suspended = true; }
        }

        void RestoreMouse()
        {
            if (_suspended && _interaction != null)
            {
                _interaction.enabled = true;
            }

            _suspended = false;
        }

        public virtual bool IsOverInterface(Vector2 screenPoint)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = screenPoint;
            _hits.Clear();
            EventSystem.current.RaycastAll(pointer, _hits);
            foreach (RaycastResult hit in _hits)
            {
                if (hit.module is GraphicRaycaster || hit.module is PanelRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Tap(Vector2 screenPoint)
        {
            if (_interaction == null || (!_interaction.enabled && !_suspended)
                || IsOverInterface(screenPoint))
            {
                return false;
            }

            AInteraction active = _interaction.GetInteraction();
            int mask = active != null ? active.GetLayerMask() : Physics.DefaultRaycastLayers;
            if (!Raycast(screenPoint, out RaycastHit hit, mask))
            {
                return false;
            }

            if (active != null)
            {
                return Activate(active, hit);
            }

            ISelectable selectable = hit.collider.GetComponentInParent<ISelectable>();
            _interaction.CancelSelection();
            _interaction.Select(selectable);
            return selectable != null;
        }

        public static bool Activate(AInteraction interaction, RaycastHit hit)
        {
            if (interaction == null || hit.collider == null || !interaction.IsValidTarget(hit.collider.gameObject))
            {
                return false;
            }

            interaction.OnMouseEnter(hit);
            interaction.OnMouseOver(hit);
            interaction.OnMouseClick(hit);
            return true;
        }
    }
}
