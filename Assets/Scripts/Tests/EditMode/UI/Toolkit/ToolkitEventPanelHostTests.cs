using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

namespace UI.Toolkit
{

    // ToolkitGameUI.Refresh returns early when part of its context is missing (here no GameManager and no player).
    // His event canvas must still be on screen in that state unless our panel has drawn a card to press.
    public class ToolkitEventPanelHostTests
    {
        GameObject _legacy;
        GameObject _host;
        Canvas _eventCanvas;
        GraphicRaycaster _eventRaycaster;
        EventView _eventView;
        PanelSettings _panelSettings;
        SceneSetup[] _scenes;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scenes = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.OpenScene(ToolkitSceneNavigation.MenuPath);
            yield return new EnterPlayMode();
            yield return WaitFrames(5);
            Object.FindAnyObjectByType<ToolkitGameUI>().enabled = false;

            _legacy = new GameObject("Event host fixture");
            UIManager ui = _legacy.AddComponent<UIManager>();
            GameObject eventGo = new GameObject("Event view", typeof(RectTransform));
            eventGo.transform.SetParent(_legacy.transform);
            _eventCanvas = eventGo.AddComponent<Canvas>();
            _eventCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _eventRaycaster = eventGo.AddComponent<GraphicRaycaster>();
            _eventView = eventGo.AddComponent<EventView>();
            GameObject container = new GameObject("Choices", typeof(RectTransform));
            container.transform.SetParent(eventGo.transform);
            TestHelpers.SetPrivateField(_eventView, "_choiceContainer", (RectTransform)container.transform);
            TestHelpers.SetPrivateField(ui, "_views", new Dictionary<ViewType, AView>
            {
                // ToolkitGameContext.Init reads the Game view; this fixture has none
                { ViewType.Game, null },
                { ViewType.Event, _eventView },
            });
            TestHelpers.SetPrivateField(ui, "_currentView", ViewType.Event);

            _host = new GameObject("Toolkit event host");
            _host.SetActive(false);
            UIDocument document = _host.AddComponent<UIDocument>();
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            document.panelSettings = _panelSettings;
            _host.AddComponent<ToolkitGameUI>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_host);
            Object.Destroy(_legacy);
            yield return null;
            Object.Destroy(_panelSettings);
            yield return new ExitPlayMode();
            bool canRestore = _scenes != null && System.Array.Exists(_scenes,
                scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path));
            if (canRestore)
            {
                EditorSceneManager.RestoreSceneManagerSetup(_scenes);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        VisualElement Root
        {
            get { return _host.GetComponent<UIDocument>().rootVisualElement; }
        }

        int EnabledCards()
        {
            int count = 0;
            Root.Q("event-list").Query<UnityEngine.UIElements.Button>("data-card").ForEach(card =>
            {
                if (card.enabledInHierarchy)
                {
                    count++;
                }
            });
            return count;
        }

        [UnityTest]
        public IEnumerator Refresh_ContextMissingAndNoActivatableChoice_LeavesHisScreenUp()
        {
            _eventView.Display("Rest", "", new List<EventChoice> { new EventChoice { label = "Resurrect", isAvailable = false } });
            LogAssert.Expect(LogType.Error, new Regex(@"^\[ToolkitEventPanel\]"));
            _host.SetActive(true);
            yield return WaitFrames(10);

            Assert.IsTrue(_host.GetComponent<ToolkitGameUI>().enabled, "The HUD built its layout.");
            Assert.IsTrue(_eventCanvas.enabled, "No card of ours to press: his screen is the way out of the room.");
            Assert.IsTrue(_eventRaycaster.enabled);
            Assert.IsTrue(Root.Q("event-panel").ClassListContains("is-hidden"));
        }

        [UnityTest]
        public IEnumerator Refresh_ContextMissing_StillRunsThePanelSoExactlyOneScreenDraws()
        {
            _eventView.Display("Rest", "Take a moment to recover",
                new List<EventChoice> { new EventChoice { label = "Heal" }, new EventChoice { label = "Leave" } });
            _host.SetActive(true);
            yield return WaitFrames(10);

            Assert.IsFalse(Root.Q("event-panel").ClassListContains("is-hidden"), "Our panel drew the room.");
            Assert.AreEqual(2, EnabledCards());
            Assert.IsFalse(_eventCanvas.enabled, "His canvas is down only because our cards are up.");
            Assert.IsFalse(_eventRaycaster.enabled);
        }

        static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
