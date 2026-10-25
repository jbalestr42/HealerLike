using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace UI.Toolkit
{
    // A full card shows its details in the desktop detail panel on a mouse hover or a keyboard focus, never on a touch
    public class ToolkitCardTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        Button _button;
        int _inspects;
        int _ended;

        [SetUp]
        public void SetUp()
        {
            _inspects = 0;
            _ended = 0;
            _panel = new ToolkitTestPanel();
            VisualElement list = new VisualElement { name = "cards" };
            _panel.root.Add(list);
            _view = new ToolkitGameView(_panel.root);
            _view.OnInspect.AddListener(model => _inspects++);
            _view.OnInspectEnded.AddListener(() => _ended++);
            _view.SetCards("cards", new[] { new ToolkitCardModel { title = "Shield" } });
            _button = list.Q<Button>("data-card");
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            _panel.Dispose();
        }

        void Send(EventBase evt)
        {
            using (evt)
            {
                evt.target = _button;
                _button.SendEvent(evt);
            }
        }

        static Event Mouse()
        {
            return new Event { type = EventType.MouseMove, mousePosition = Vector2.one };
        }

        static Touch Finger()
        {
            return new Touch { fingerId = 0, position = Vector2.one, phase = TouchPhase.Began };
        }

        void Desktop()
        {
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(1440f, 900f)));
        }

        [Test]
        public void MouseHover_DesktopLayout_ShowsAndEndsTheDetails()
        {
            Desktop();
            Send(PointerEnterEvent.GetPooled(Mouse()));
            Send(PointerLeaveEvent.GetPooled(Mouse()));
            Assert.AreEqual(1, _inspects);
            Assert.AreEqual(1, _ended);
        }

        // A tablet laid out as a desktop: the touch enters on its press, which is no hover
        [Test]
        public void TouchEnter_DesktopLayout_ShowsNothing()
        {
            Desktop();
            Send(PointerEnterEvent.GetPooled(Finger()));
            Send(PointerLeaveEvent.GetPooled(Finger()));
            Assert.AreEqual(0, _inspects);
            Assert.AreEqual(0, _ended);
        }

        // The phone layout has no detail panel to show them in
        [Test]
        public void MouseHover_PhoneLayout_ShowsNothing()
        {
            _view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
            _view.RecordPointer(PointerType.mouse);
            Send(PointerEnterEvent.GetPooled(Mouse()));
            Assert.AreEqual(0, _inspects);
        }

        [Test]
        public void Focus_AfterAMouse_ShowsTheDetails()
        {
            Desktop();
            _view.RecordPointer(PointerType.mouse);
            _button.Focus();
            Assert.AreEqual(1, _inspects);
        }

        // A tap focuses the card it presses: that focus is no keyboard navigation
        [Test]
        public void Focus_AfterATouch_ShowsNothing()
        {
            Desktop();
            _view.RecordPointer(PointerType.touch);
            _button.Focus();
            Assert.AreEqual(0, _inspects);
        }
    }
}
