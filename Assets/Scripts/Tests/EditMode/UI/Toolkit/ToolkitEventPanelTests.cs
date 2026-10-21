using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UiButton = UnityEngine.UI.Button;

namespace UI.Toolkit
{

    // The event and rest choices of Julien's EventView become Toolkit cards, and his canvas is the fallback
    public class ToolkitEventPanelTests
    {
        static readonly Regex PanelError = new Regex(@"^\[ToolkitEventPanel\]");

        GameObject _host;
        GameObject _eventGo;
        Canvas _eventCanvas;
        GraphicRaycaster _eventRaycaster;
        EventView _eventView;
        UIManager _ui;
        ToolkitGameContext _context;
        VisualElement _root;
        ToolkitGameView _view;
        ToolkitLegacyCanvases _legacyCanvases;
        ToolkitEventPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Event adapter fixture");
            _ui = _host.AddComponent<UIManager>();
            _eventGo = new GameObject("Event view", typeof(RectTransform));
            _eventGo.transform.SetParent(_host.transform);
            _eventCanvas = _eventGo.AddComponent<Canvas>();
            _eventCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _eventRaycaster = _eventGo.AddComponent<GraphicRaycaster>();
            TestHelpers.WithLoggingDisabled(() => _eventView = _eventGo.AddComponent<EventView>());
            GameObject container = new GameObject("Choices", typeof(RectTransform));
            container.transform.SetParent(_eventGo.transform);
            TestHelpers.SetPrivateField(_eventView, "_choiceContainer", (RectTransform)container.transform);
            TestHelpers.SetPrivateField(_ui, "_views", new Dictionary<ViewType, AView> { { ViewType.Event, _eventView } });
            TestHelpers.SetPrivateField(_ui, "_currentView", ViewType.Event);
            _context = new ToolkitGameContext();
            _context.ui = _ui;
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(_root);
            _legacyCanvases = new ToolkitLegacyCanvases();
            _legacyCanvases.Hide(_context);
            _panel = new ToolkitEventPanel();
            _panel.findAsset = label => null;
            _panel.Init(null, _context, _view, _legacyCanvases);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            Object.DestroyImmediate(_host);
        }

        static EventChoice Choice(string label, System.Action onSelected = null, bool isAvailable = true,
            string description = null)
        {
            return new EventChoice
            {
                label = label,
                description = description ?? $"{label} description",
                isAvailable = isAvailable,
                onSelected = onSelected,
            };
        }

        void Display(params EventChoice[] choices)
        {
            _eventView.Display("Rest", "Take a break", choices);
        }

        List<string> CardTitles()
        {
            return _root.Q("event-list").Query<Label>("card-title").ToList().Select(label => label.text).ToList();
        }

        bool IsPanelHidden()
        {
            return _root.Q("event-panel").ClassListContains("is-hidden");
        }

        [Test]
        public void BuildModels_RestChoices_OneCardPerChoiceWithItsLabelDescriptionAndAvailability()
        {
            Display(Choice("Heal", description: "Every unit recovers 30% of its max health."), Choice("Resurrect"));

            List<ToolkitCardModel> models = _panel.BuildModels(_eventView.choiceButtons);

            Assert.AreEqual(2, models.Count);
            Assert.AreEqual("Heal", models[0].title);
            Assert.AreEqual("Every unit recovers 30% of its max health.", models[0].description);
            Assert.IsTrue(models[0].isEnabled);
            Assert.AreSame(_eventView.choiceButtons[0], models[0].source);
            Assert.IsNotNull(models[0].activate);
            Assert.AreEqual("Resurrect", models[1].title);
        }

        [Test]
        public void BuildModels_UnavailableChoice_IsADisabledCardWithAnUnavailableStatus()
        {
            Display(Choice("Heal"), Choice("Resurrect", isAvailable: false));

            List<ToolkitCardModel> models = _panel.BuildModels(_eventView.choiceButtons);

            Assert.IsFalse(models[1].isEnabled);
            Assert.AreEqual("Unavailable", models[1].status);
        }

        [Test]
        public void BuildModels_ChoiceWithoutDescription_HasAnEmptyDescription()
        {
            _eventView.Display("Rest", "", new List<EventChoice> { new EventChoice { label = "Back" } });

            List<ToolkitCardModel> models = _panel.BuildModels(_eventView.choiceButtons);

            Assert.AreEqual(string.Empty, models[0].description);
        }

        [Test]
        public void BuildModels_FoundAsset_BecomesTheIconSource()
        {
            object asset = new object();
            _panel.findAsset = label => label == "Heal" ? asset : null;
            Display(Choice("Heal"), Choice("Leave"));

            List<ToolkitCardModel> models = _panel.BuildModels(_eventView.choiceButtons);

            Assert.AreSame(asset, models[0].iconSource);
            Assert.IsInstanceOf<ToolkitEventPanel.ChoiceIcon>(models[1].iconSource);
        }

        [Test]
        public void BuildModels_NoAsset_UsesAProceduralIconLabelledWithTheChoice()
        {
            Display(Choice("Leave"));

            ToolkitCardModel model = _panel.BuildModels(_eventView.choiceButtons)[0];

            IDataIconMetadata icon = model.iconSource as IDataIconMetadata;
            Assert.IsNotNull(icon);
            Assert.AreEqual("Leave", icon.label);
            Assert.IsNotNull(_view.GetIcon(model.iconSource, out bool isPortrait));
            Assert.IsFalse(isPortrait);
        }

        [Test]
        public void Activate_Card_InvokesHisChoiceOnceAndNeverTwice()
        {
            int picked = 0;
            Display(Choice("Heal", () => picked++));
            ToolkitCardModel model = _panel.BuildModels(_eventView.choiceButtons)[0];

            model.activate.Invoke(model);
            model.activate.Invoke(model);

            Assert.AreEqual(1, picked, "His own Select counts the first pick only.");
        }

        [Test]
        public void Activate_ChoiceButtonDestroyed_InvokesNothingAndWarns()
        {
            int picked = 0;
            Display(Choice("Heal", () => picked++));
            ToolkitCardModel model = _panel.BuildModels(_eventView.choiceButtons)[0];
            Object.DestroyImmediate(((UiButton)model.source).gameObject);
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[ToolkitEventPanel\]"));

            model.activate.Invoke(model);

            Assert.AreEqual(0, picked);
        }

        [Test]
        public void Refresh_ActivatableChoices_DrawsOurPanelAndHidesHisCanvas()
        {
            Display(Choice("Heal"), Choice("Resurrect", isAvailable: false));

            _panel.Refresh();

            Assert.IsTrue(_panel.isDrawing);
            Assert.IsFalse(IsPanelHidden());
            CollectionAssert.AreEqual(new[] { "Heal", "Resurrect" }, CardTitles());
            Assert.IsFalse(_eventCanvas.enabled);
            Assert.IsFalse(_eventRaycaster.enabled);
        }

        [Test]
        public void Refresh_NoActivatableChoice_KeepsHisCanvasVisibleAndLogsOneError()
        {
            Display(Choice("Resurrect", isAvailable: false));
            LogAssert.Expect(LogType.Error, PanelError);

            _panel.Refresh();
            _panel.Refresh();

            Assert.IsFalse(_panel.isDrawing);
            Assert.IsTrue(IsPanelHidden());
            Assert.IsTrue(_eventCanvas.enabled, "His canvas is the only way out of the room.");
            Assert.IsTrue(_eventRaycaster.enabled);
        }

        [Test]
        public void Refresh_NoChoiceAtAll_KeepsHisCanvasVisibleWithoutAnError()
        {
            _eventView.Display("Rest", "", new List<EventChoice>());

            _panel.Refresh();

            Assert.IsFalse(_panel.isDrawing);
            Assert.IsTrue(_eventCanvas.enabled);
        }

        [Test]
        public void Refresh_FallbackThenAnActivatableChoice_TakesOver()
        {
            Display(Choice("Resurrect", isAvailable: false));
            LogAssert.Expect(LogType.Error, PanelError);
            _panel.Refresh();

            Display(Choice("Heal"));
            _panel.Refresh();

            Assert.IsTrue(_panel.isDrawing);
            Assert.IsFalse(_eventCanvas.enabled);
        }

        [Test]
        public void Refresh_EventViewDisplayedAgain_ReplacesTheCards()
        {
            Display(Choice("Heal"), Choice("Resurrect"));
            _panel.Refresh();

            Display(Choice("Zealot"), Choice("Back"));
            _panel.Refresh();

            CollectionAssert.AreEqual(new[] { "Zealot", "Back" }, CardTitles());
        }

        [Test]
        public void Refresh_ChoiceBecomesUnavailable_RedrawsItsCard()
        {
            Display(Choice("Heal"), Choice("Resurrect"));
            _panel.Refresh();

            _eventView.choiceButtons[1].interactable = false;
            _panel.Refresh();

            Assert.IsTrue(_root.Q("event-list").Query(className: "is-disabled").ToList().Count == 1);
        }

        [Test]
        public void Refresh_LeavingTheEventView_HidesOurPanelAndPutsHisCanvasBack()
        {
            Display(Choice("Heal"));
            _panel.Refresh();

            TestHelpers.SetPrivateField(_ui, "_currentView", ViewType.Game);
            _panel.Refresh();

            Assert.IsFalse(_panel.isDrawing);
            Assert.IsTrue(IsPanelHidden());
            Assert.IsTrue(_eventCanvas.enabled, "Off his event screen his canvas is as the scene had it.");
            Assert.IsTrue(_eventRaycaster.enabled);
        }

        [Test]
        public void HiddenLegacyScreens_PanelNeverRefreshed_LeaveHisChoicesOnScreen()
        {
            // The state of a HUD refresh that returns before the panel: Hide has run, the panel has not
            Display(Choice("Heal"), Choice("Leave"));

            Assert.IsFalse(_panel.isDrawing);
            Assert.IsTrue(IsPanelHidden());
            Assert.IsTrue(_eventCanvas.enabled, "With no card of ours drawn, his screen is the way out of the room.");
            Assert.IsTrue(_eventRaycaster.enabled);
        }

        [Test]
        public void Refresh_SameChoicesDisplayedAgain_RebindsTheCardsToHisNewButtons()
        {
            Display(Choice("Heal"), Choice("Resurrect"));
            _panel.Refresh();

            // A resurrect pick pops his view and displays the same labels again, with new button objects
            int picked = 0;
            Display(Choice("Heal", () => picked++), Choice("Resurrect"));
            _panel.Refresh();
            UnityEngine.UIElements.Button card = _root.Q("event-list").Query<UnityEngine.UIElements.Button>("data-card")
                .ToList().First(button => button.Q<Label>("card-title")?.text == "Heal");
            ToolkitCardModel model = (ToolkitCardModel)card.userData;

            Assert.AreSame(_eventView.choiceButtons[0], model.source);
            model.activate.Invoke(model);

            Assert.AreEqual(1, picked, "The card drawn after the redisplay presses his new button, not the destroyed one.");
        }

        [Test]
        public void Refresh_ScreenTextOfHisView_FillsOurTitleAndDescription()
        {
            TMP_Text title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            TMP_Text description = new GameObject("Description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(_eventGo.transform);
            description.transform.SetParent(_eventGo.transform);
            TestHelpers.SetPrivateField(_eventView, "_title", title);
            TestHelpers.SetPrivateField(_eventView, "_description", description);
            Display(Choice("Heal"));

            _panel.Refresh();

            Assert.AreEqual("Rest", _root.Q<Label>("event-title").text);
            Assert.AreEqual("Take a break", _root.Q<Label>("event-description").text);
            Assert.IsFalse(_root.Q("event-description").ClassListContains("is-hidden"));
        }

        [Test]
        public void Refresh_HostKeepsHisCanvases_DrawsNothing()
        {
            ToolkitEventPanel hosted = new ToolkitEventPanel();
            hosted.Init(null, _context, _view, null);
            _eventCanvas.enabled = true;
            Display(Choice("Heal"));

            hosted.Refresh();

            Assert.IsFalse(hosted.isDrawing);
            Assert.IsTrue(IsPanelHidden());
            Assert.IsTrue(_eventCanvas.enabled);
        }
    }
}
