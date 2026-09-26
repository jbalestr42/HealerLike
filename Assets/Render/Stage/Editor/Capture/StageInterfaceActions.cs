using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceActions
    {
        public ToolkitGameUI ui;
        public int scrollActions { get; private set; }
        public VisualElement root { get { return ui.GetComponent<UIDocument>().rootVisualElement; } }
        public StageTouchInput touch { get { return ui.GetComponent<StageTouchInput>(); } }
        StageCaptureInput _input;
        public bool legacyModuleReadTouches
        {
            get
            {
                return _input != null && _input.samplesRead > 0 && EventSystem.current != null
                    && EventSystem.current.currentInputModule is StandaloneInputModule module
                    && module.inputOverride == _input;
            }
        }

        public void ConfigureLegacyInput()
        {
            StandaloneInputModule module = StageLegacyInput.Configure(ui.gameObject.scene);
            if (module == null)
            {
                throw new InvalidOperationException("The capture scene has no EventSystem for legacy touch input.");
            }
            _input = ui.gameObject.AddComponent<StageCaptureInput>();
            module.inputOverride = _input;
        }

        public void Submit(string name)
        {
            Submit(root.Q<Button>(name));
        }

        public void Submit(Button button)
        {
            RequireReachable(button);
            button.Focus();
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                button.SendEvent(submit);
            }
        }

        static void RequireReachable(Button button)
        {
            if (button == null || !button.enabledInHierarchy || !StageInterfaceOutput.IsVisible(button))
            {
                throw new InvalidOperationException("UI button unavailable: " + (button != null ? button.name : "null"));
            }
            VisualElement picked = button.panel.Pick(button.worldBound.center);
            bool reachable = false;
            for (VisualElement target = picked; target != null; target = target.parent)
            {
                if (target == button)
                {
                    reachable = true;
                    break;
                }
            }
            if (!reachable)
            {
                throw new InvalidOperationException("UI button is covered or outside its scroll viewport: " + button.name);
            }
        }

        public IEnumerator PointerTap(string name)
        {
            yield return PointerTap(root.Q<Button>(name));
        }

        public IEnumerator PointerTap(Button button)
        {
            RequireReachable(button);
            yield return TouchGesture(ScreenPoint(button));
        }

        public IEnumerator SelectCardByTouch(Button button)
        {
            yield return BringIntoView(button);
            yield return PointerTap(button);
        }

        public IEnumerator TouchGesture(Vector2 point)
        {
            StageTouchInput input = touch;
            try
            {
                yield return TouchFrame(input, TouchPhase.Began, point);
                // Keep the finger down across the HUD's 0.1 second refresh, then release.
                float deadline = Time.realtimeSinceStartup + 0.16f;
                do
                {
                    yield return TouchFrame(input, TouchPhase.Stationary, point);
                }
                while (Time.realtimeSinceStartup < deadline);
                yield return TouchFrame(input, TouchPhase.Moved, point + Vector2.right);
                yield return TouchFrame(input, TouchPhase.Ended, point);
                if (input != null)
                {
                    input.captureTouches = Array.Empty<Touch>();
                }
                _input.samples = Array.Empty<Touch>();
                yield return null;
            }
            finally
            {
                if (input != null)
                {
                    input.captureTouches = null;
                }
                _input.samples = Array.Empty<Touch>();
            }
        }

        IEnumerator TouchFrame(StageTouchInput input, TouchPhase phase, Vector2 point)
        {
            Touch sample = new Touch { fingerId = 0, phase = phase, position = point, tapCount = 1 };
            _input.samples = new[] { sample };
            if (input != null)
            {
                input.captureTouches = _input.samples;
            }
            // StageTouchInput and StandaloneInputModule both process the sample before our next LateUpdate.
            yield return null;
        }

        public IEnumerator BringIntoView(Button button)
        {
            ScrollView scroll = button.GetFirstAncestorOfType<ScrollView>();
            if (scroll != null)
            {
                Vector2 previous = scroll.scrollOffset;
                scroll.ScrollTo(button);
                yield return null;
                yield return null;
                if (Vector2.Distance(previous, scroll.scrollOffset) > 0.1f)
                {
                    scrollActions++;
                }
            }
        }

        public IEnumerator SelectCard(Button button)
        {
            yield return BringIntoView(button);
            Submit(button);
        }

        public List<Button> Cards(string list)
        {
            return root.Q(list).Query<Button>("data-card").ToList();
        }

        public void WorldTap(Vector2 point)
        {
            touch.ProcessTouch(0, TouchPhase.Began, point);
            touch.ProcessTouch(0, TouchPhase.Ended, point);
        }

        public static Vector2 ScreenPoint(VisualElement element)
        {
            Vector2 origin = RuntimePanelUtils.ScreenToPanel(element.panel, Vector2.zero);
            Vector2 end = RuntimePanelUtils.ScreenToPanel(element.panel, new Vector2(Screen.width, Screen.height));
            Vector2 point = element.worldBound.center;
            return new Vector2((point.x - origin.x) / (end.x - origin.x) * Screen.width,
                Screen.height - (point.y - origin.y) / (end.y - origin.y) * Screen.height);
        }
    }
}
