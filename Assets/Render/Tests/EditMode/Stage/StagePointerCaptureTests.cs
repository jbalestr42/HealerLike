using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StagePointerCaptureTests
    {
        [UnityTest]
        public IEnumerator CapturedRosterPointerCanLeaveUiButReturningToCardStillBlocksDrop()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            var host = new GameObject("Captured roster test");
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var system = host.AddComponent<EventSystem>();
            host.AddComponent<StandaloneInputModule>();
            var input = host.AddComponent<StageTouchInput>();
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;
            var root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = root.style.top = root.style.right = root.style.bottom = 0;
            var card = new VisualElement();
            card.style.width = card.style.height = 80;
            root.Add(card);
            yield return null;
            yield return null;
            root.panel.visualTree.pickingMode = PickingMode.Ignore;
            Vector2 onCard = ScreenPoint(root.panel, card.worldBound.center);
            Vector2 onBoard = ScreenPoint(root.panel, new Vector2(180, 180));
            Assert.That(input.IsOverInterface(onCard), Is.True);
            Assert.That(input.IsOverInterface(onBoard), Is.False);
            card.CapturePointer(PointerId.mousePointerId);
            yield return null;
            Assert.That(card.HasPointerCapture(PointerId.mousePointerId), Is.True);
            var hits = new List<RaycastResult>();
            system.RaycastAll(new PointerEventData(system) { pointerId = -1, position = onBoard }, hits);
            Assert.That(hits.Exists(hit => hit.module is PanelRaycaster), Is.True,
                "Reproduce the captured panel hit outside the roster.");
            Assert.That(input.IsOverInterface(onBoard), Is.False,
                "Pointer capture must not reject a release over the board.");
            Assert.That(input.IsOverInterface(onCard), Is.True,
                "A release back on the roster must still be rejected.");
            card.ReleasePointer(PointerId.mousePointerId);
            Object.Destroy(host);
            Object.Destroy(settings);
            yield return null;
            yield return new ExitPlayMode();
        }

        static Vector2 ScreenPoint(IPanel panel, Vector2 point)
        {
            Vector2 origin = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            Vector2 size = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height)) - origin;
            return new Vector2((point.x - origin.x) / size.x * Screen.width,
                Screen.height - (point.y - origin.y) / size.y * Screen.height);
        }
    }
}
