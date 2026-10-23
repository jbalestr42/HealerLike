using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // An element of the HUD explained in the popover: a mouse opens it on hover and closes it on leave, a touch on tap
    public class ToolkitInfoTriggerTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        VisualElement _parent;
        VisualElement _element;
        ToolkitInfoTrigger _trigger;
        ToolkitCardModel _model;
        readonly List<ToolkitCardModel> _requested = new List<ToolkitCardModel>();
        int _closed;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _parent = new VisualElement();
            _element = new VisualElement { pickingMode = PickingMode.Ignore };
            _parent.Add(_element);
            _panel.root.Add(_parent);
            _view = new ToolkitGameView(_panel.root);
            _requested.Clear();
            _closed = 0;
            _model = new ToolkitCardModel { title = "Mana 12 / 30" };
            _view.OnInspectRequested.AddListener(_requested.Add);
            _view.OnClosePopover += OnClosed;
            _trigger = new ToolkitInfoTrigger(_view, _element, () => _model);
        }

        [TearDown]
        public void TearDown()
        {
            _trigger.Dispose();
            _view.OnClosePopover -= OnClosed;
            _view.Release();
            _panel.Dispose();
        }

        void OnClosed()
        {
            _closed++;
        }

        static void Send(VisualElement target, EventBase evt)
        {
            using (evt)
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        [Test]
        public void New_ElementIgnoringThePointer_CanBeHovered()
        {
            Assert.AreEqual(PickingMode.Position, _element.pickingMode);
        }

        [Test]
        public void PointerEnter_AsksThePopoverForTheModel()
        {
            Send(_element, PointerEnterEvent.GetPooled());

            CollectionAssert.AreEqual(new[] { _model }, _requested);
        }

        [Test]
        public void PointerLeave_ClosesThePopover()
        {
            Send(_element, PointerLeaveEvent.GetPooled());

            Assert.AreEqual(1, _closed);
        }

        [Test]
        public void PointerDown_OpensAndStopsBeforeTheParent()
        {
            int reached = 0;
            _parent.RegisterCallback<PointerDownEvent>(evt => reached++);

            Send(_element, PointerDownEvent.GetPooled());

            Assert.AreEqual(1, _requested.Count);
            Assert.AreEqual(0, reached);
        }

        [Test]
        public void Open_NoModel_OpensNothing()
        {
            _model = null;

            Assert.IsFalse(_trigger.Open());
            Assert.IsEmpty(_requested);
        }

        [Test]
        public void Dispose_StopsOpening()
        {
            _trigger.Dispose();

            Send(_element, PointerEnterEvent.GetPooled());

            Assert.IsEmpty(_requested);
        }
    }
}
