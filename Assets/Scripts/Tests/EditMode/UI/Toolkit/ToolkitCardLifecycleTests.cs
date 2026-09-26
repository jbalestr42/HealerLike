using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    public class ToolkitCardLifecycleTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        VisualElement _list;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _list = new VisualElement();
            _list.name = "party-list";
            _panel.root.Add(_list);
            _view = new ToolkitGameView(_panel.root);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            _panel.Dispose();
        }

        [Test]
        public void Init_MissingTemplate_ReportsErrorWithoutFallbackControls()
        {
            ToolkitCard card = new ToolkitCard();
            LogAssert.Expect(LogType.Error, "[ToolkitTemplates] Missing template for 'card-shell'.");
            card.Init(_view, null);
            Assert.IsNull(card.root);
            Assert.IsNull(card.button);
            card.Dispose();
        }

        [Test]
        public void Refresh_ReusedCard_ActivatesAndInspectsOnlyCurrentModel()
        {
            int oldActions = 0;
            int newActions = 0;
            ToolkitCardModel inspected = null;
            ToolkitCardModel oldModel = new ToolkitCardModel();
            oldModel.activate = delegate
            {
                oldActions++;
            };
            ToolkitCardModel current = new ToolkitCardModel();
            current.activate = delegate
            {
                newActions++;
            };
            _view.OnInspectRequested.AddListener(model => inspected = model);
            _view.SetCards("party-list", new[] { oldModel });
            Button button = _list.Q<Button>("data-card");
            _view.SetCards("party-list", new[] { current });
            ToolkitTestPanel.Submit(button);
            ToolkitTestPanel.Submit(_list.Q<Button>("card-info"));
            Assert.AreEqual(0, oldActions);
            Assert.AreEqual(1, newActions);
            Assert.AreSame(current, inspected);
            Assert.AreSame(button, _list.Q<Button>("data-card"));
        }

        [Test]
        public void Release_RetainedCardReattached_DoesNotInvokeOldCallbacks()
        {
            int actions = 0;
            int inspections = 0;
            ToolkitCardModel model = new ToolkitCardModel();
            model.activate = delegate
            {
                actions++;
            };
            _view.OnInspectRequested.AddListener(
                delegate
                {
                    inspections++;
                }
            );
            _view.SetCards("party-list", new[] { model });
            VisualElement card = _list[0];
            _view.Release();
            _panel.root.Add(card);
            ToolkitTestPanel.Submit(card.Q<Button>("data-card"));
            ToolkitTestPanel.Submit(card.Q<Button>("card-info"));
            Assert.AreEqual(0, actions);
            Assert.AreEqual(0, inspections);
        }

        [Test]
        public void Activate_CallbackReleasesView_CompletesWithoutLateNotification()
        {
            int actions = 0;
            int notifications = 0;
            ToolkitCardModel model = new ToolkitCardModel();
            model.activate = delegate
            {
                actions++;
                _view.Release();
            };
            _view.OnCardActivated.AddListener(
                delegate
                {
                    notifications++;
                }
            );
            _view.SetCards("party-list", new[] { model });
            ToolkitTestPanel.Submit(_list.Q<Button>("data-card"));
            Assert.AreEqual(1, actions);
            Assert.AreEqual(0, notifications);
            Assert.AreEqual(0, _list.childCount);
        }

        [Test]
        public void Release_LateBindingAndIconRequests_DoNotReactivateView()
        {
            Button button = new Button();
            button.name = "action";
            _panel.root.Add(button);
            int calls = 0;
            _view.AddClickListener(
                "action",
                delegate
                {
                    calls++;
                }
            );
            _view.Release();
            _view.AddClickListener(
                "action",
                delegate
                {
                    calls++;
                }
            );
            _view.SetCards("party-list", new[] { new ToolkitCardModel() });
            ToolkitTestPanel.Submit(button);
            Texture2D icon = _view.GetIcon(null, out bool isPortrait);
            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _list.childCount);
            Assert.IsNull(icon);
            Assert.IsFalse(isPortrait);
        }
    }
}
