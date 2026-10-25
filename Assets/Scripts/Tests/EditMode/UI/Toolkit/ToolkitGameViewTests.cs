using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace UI.Toolkit
{

    public class ToolkitGameViewTests
    {
        VisualElement _root;
        VisualElement _list;
        ToolkitGameView _view;

        static ToolkitCardModel CreateModel(string title, bool isEnabled = true)
        {
            ToolkitCardModel model = new ToolkitCardModel();
            model.title = title;
            model.isEnabled = isEnabled;
            return model;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _list = new VisualElement();
            _list.name = "cards";
            _root.Add(_list);
            _view = new ToolkitGameView(_root);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
        }

        [Test]
        public void SetCards_TwoModels_AddsTwoCards()
        {
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Heal"), CreateModel("Guard") });
            Assert.AreEqual(2, _list.childCount);
        }

        [Test]
        public void SetCards_FewerModels_ReusesFirstCardAndRemovesStale()
        {
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Heal"), CreateModel("Guard") });
            VisualElement first = _list[0];
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("New spell", isEnabled: false) });
            Assert.AreEqual(1, _list.childCount);
            Assert.AreSame(first, _list[0]);
            Assert.IsFalse(first.Q<Button>("data-card").enabledSelf);
            Assert.AreEqual("New spell", first.Q<Label>(className: "data-card__title").text);
        }

        [Test]
        public void SetCards_NoIconSource_ShowsFallbackIcon()
        {
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Unknown data") });
            Assert.IsNotNull(_list[0].Q(className: "data-card__icon").style.backgroundImage.value.texture);
        }

        [Test]
        public void Show_HiddenThenShown_RemovesHiddenClass()
        {
            VisualElement panel = new VisualElement();
            panel.name = "inventory-panel";
            _root.Add(panel);
            _view.Show("inventory-panel", false);
            bool wasHidden = panel.ClassListContains("is-hidden");
            _view.Show("inventory-panel", true);
            Assert.IsTrue(wasHidden);
            Assert.IsFalse(panel.ClassListContains("is-hidden"));
        }

        [Test]
        public void SetResource_HalfOfMaximum_SetsFiftyPercent()
        {
            ProgressBar bar = new ProgressBar();
            bar.name = "mana-bar";
            _root.Add(bar);
            _view.SetResource("mana-bar", 30f, 60f);
            Assert.AreEqual(50f, bar.value);
            Assert.AreEqual("30 / 60", bar.title);
        }

        [Test]
        public void ShowDetail_Model_SetsTitleAndDescription()
        {
            Label title = new Label();
            title.name = "detail-title";
            Label description = new Label();
            description.name = "detail-description";
            _root.Add(title);
            _root.Add(description);
            ToolkitCardModel model = CreateModel("Guard");
            model.description = "Protects an ally";
            _view.ShowDetail(model);
            Assert.AreEqual("Guard", title.text);
            Assert.AreEqual("Protects an ally", description.text);
        }

        [Test]
        public void Load_GameUIAndDataCard_ResolveFromResources()
        {
            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
            VisualTreeAsset card = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
            TemplateContainer cardTree = card.CloneTree();
            Assert.IsNotNull(layout);
            Assert.IsNotNull(cardTree.Q<Button>("data-card"));
            Assert.IsNotNull(cardTree.Q("card-icon"));
        }

        [Test]
        public void SetLayout_SameStateTwice_AnnouncesOnce()
        {
            int changes = 0;
            _view.OnLayoutChanged += delegate { changes++; };
            ToolkitLayoutState phone = ToolkitLayout.FromPanel(new Vector2(390f, 844f));
            Assert.IsTrue(_view.SetLayout(phone));
            Assert.IsFalse(_view.SetLayout(phone));
            Assert.AreEqual(1, changes);
            Assert.IsTrue(_view.isPhoneLayout);
        }

        [Test]
        public void SetLayout_Rotated_AnnouncesTheNewState()
        {
            ToolkitLayoutState received = default;
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
            _view.OnLayoutChanged += delegate(ToolkitLayoutState state) { received = state; };
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(844f, 390f)));
            Assert.AreEqual(ToolkitLayoutMode.Landscape, received.mode);
            Assert.AreEqual(ToolkitLayoutMode.Landscape, _view.layout.mode);
        }

        // Before any pointer, the form factor stands for it: a phone is touched, a desktop has a mouse
        [Test]
        public void IsTouchInput_NoPointerYet_FollowsTheFormFactor()
        {
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
            Assert.IsTrue(_view.isTouchInput);
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(1440f, 900f)));
            Assert.IsFalse(_view.isTouchInput);
        }

        // A tablet in landscape gets the desktop layout, but it is touched until a mouse says otherwise
        [Test]
        public void IsTouchInput_NoPointerYetOnTabletDesktopLayout_IsTouch()
        {
            ToolkitLayoutState tablet = ToolkitLayout.Resolve(new Vector2Int(2160, 1620),
                new Rect(0f, 0f, 2160f, 1620f), true);
            _view.SetLayout(tablet);
            Assert.IsFalse(_view.isPhoneLayout);
            Assert.IsTrue(_view.isTouchInput);
            _view.RecordPointer(PointerType.mouse);
            Assert.IsFalse(_view.isTouchInput);
        }

        // A narrow PC window takes the portrait layout but keeps its mouse hover
        [Test]
        public void RecordPointer_MouseOnPhoneLayout_StaysMouseAcrossLayoutChanges()
        {
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
            _view.RecordPointer(PointerType.mouse);
            Assert.IsFalse(_view.isTouchInput);
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(844f, 390f)));
            Assert.IsFalse(_view.isTouchInput);
            Assert.IsTrue(_view.isPhoneLayout);
        }

        [Test]
        public void RecordPointer_TouchOrPenOnDesktopLayout_IsTouch()
        {
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(1440f, 900f)));
            _view.RecordPointer(PointerType.touch);
            Assert.IsTrue(_view.isTouchInput);
            _view.RecordPointer(PointerType.mouse);
            _view.RecordPointer(PointerType.pen);
            Assert.IsTrue(_view.isTouchInput);
        }

        static void Send(VisualElement target, EventBase evt)
        {
            using (evt)
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        static Touch Press(Vector2 point)
        {
            return new Touch { fingerId = 0, position = point, phase = TouchPhase.Began };
        }

        static Event MouseMove(Vector2 point, Vector2 delta)
        {
            return new Event { type = EventType.MouseMove, mousePosition = point, delta = delta };
        }

        // The view reads the pointer type of every press and mouse move on the interface
        [Test]
        public void PointerEvents_TouchPressThenMouseMove_FollowTheLastPointer()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                VisualElement target = new VisualElement();
                panel.root.Add(target);
                ToolkitGameView view = new ToolkitGameView(panel.root);
                view.SetLayout(ToolkitLayout.FromPanel(new Vector2(1440f, 900f)));
                Send(target, PointerDownEvent.GetPooled(Press(Vector2.one)));
                Assert.IsTrue(view.isTouchInput);
                Send(target, PointerMoveEvent.GetPooled(MouseMove(Vector2.one, Vector2.zero)));
                Assert.IsTrue(view.isTouchInput, "a mouse event that did not move is no mouse use");
                Send(target, PointerMoveEvent.GetPooled(MouseMove(Vector2.one * 4f, Vector2.one * 3f)));
                Assert.IsFalse(view.isTouchInput);
                view.Release();
            }
        }

        // The legacy mouse copy of a touch is refused by the pointer gate: it must not pass for a mouse
        [Test]
        public void PointerEvents_RefusedByTheGate_AreNotRecorded()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                VisualElement target = new VisualElement();
                panel.root.Add(target);
                ToolkitGameView view = new ToolkitGameView(panel.root);
                view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
                view.canBeginPointer = pointer => pointer != PointerId.mousePointerId;
                Send(target, PointerMoveEvent.GetPooled(MouseMove(Vector2.one * 4f, Vector2.one * 3f)));
                Send(target, PointerDownEvent.GetPooled(
                    new Event { type = EventType.MouseDown, mousePosition = Vector2.one }));
                Assert.IsTrue(view.isTouchInput);
                view.Release();
            }
        }

        // The first layout is announced even when it equals the default state
        [Test]
        public void SetLayout_FirstState_IsAnnounced()
        {
            int changes = 0;
            _view.OnLayoutChanged += delegate { changes++; };
            _view.SetLayout(default(ToolkitLayoutState));
            Assert.AreEqual(1, changes);
        }
    }
}
