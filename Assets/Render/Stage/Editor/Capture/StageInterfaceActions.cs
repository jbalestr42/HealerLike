using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceActions
    {
        public ToolkitGameUI ui;
        public int scrollActions { get; private set; }
        public VisualElement root { get { return ui.GetComponent<UIDocument>().rootVisualElement; } }
        public StageTouchInput touch { get { return ui.GetComponent<StageTouchInput>(); } }

        public void Submit(string name)
        {
            Submit(root.Q<Button>(name));
        }

        public void Submit(Button button)
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
            button.Focus();
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                button.SendEvent(submit);
            }
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
