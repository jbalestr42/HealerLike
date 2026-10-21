using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The kit chips of a class card: one per entry, their icon through the view, details through the popover route
    public class ToolkitKitRowTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        VisualElement _host;
        ToolkitKitRow _row;
        readonly List<ToolkitCardModel> _requested = new List<ToolkitCardModel>();
        int _closed;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _host = new VisualElement();
            _panel.root.Add(_host);
            _view = new ToolkitGameView(_panel.root);
            _requested.Clear();
            _closed = 0;
            _view.OnInspectRequested.AddListener(_requested.Add);
            _view.OnClosePopover += OnClosed;
            _row = new ToolkitKitRow(_view, _host);
        }

        [TearDown]
        public void TearDown()
        {
            _row.Dispose();
            _view.OnClosePopover -= OnClosed;
            _view.Release();
            _panel.Dispose();
        }

        void OnClosed()
        {
            _closed++;
        }

        static ToolkitKitEntry Entry(string title, bool showLabel = false)
        {
            return new ToolkitKitEntry { iconSource = new ToolkitClassSelect.RandomChoice(), title = title, body = title + " details", showLabel = showLabel };
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
        public void Refresh_Entries_MakesOneChipEachAndShowsTheRow()
        {
            _row.Refresh(new[] { Entry("Heal"), Entry("Zealot", true) });

            Assert.AreEqual(2, _row.count);
            Assert.AreEqual(DisplayStyle.Flex, _host.style.display.value);
            Assert.AreEqual("", _row.ChipAt(0).Q<Label>(null, ToolkitKitRow.LabelClass).text, "A skill is its icon alone.");
            Assert.AreEqual("Zealot", _row.ChipAt(1).Q<Label>(null, ToolkitKitRow.LabelClass).text);
        }

        [Test]
        public void Refresh_FewerEntries_RemovesTheOldChips()
        {
            _row.Refresh(new[] { Entry("Heal"), Entry("Shield") });

            _row.Refresh(new[] { Entry("Heal") });

            Assert.AreEqual(1, _row.count);
            Assert.AreEqual(1, _host.childCount);
        }

        [Test]
        public void Refresh_NoEntries_HidesTheRow()
        {
            _row.Refresh(new[] { Entry("Heal") });

            _row.Refresh(null);

            Assert.AreEqual(0, _row.count);
            Assert.AreEqual(DisplayStyle.None, _host.style.display.value);
        }

        [Test]
        public void Refresh_Entry_ShowsADataIconThroughTheView()
        {
            _row.Refresh(new[] { Entry("Heal") });

            Texture2D icon = _row.ChipAt(0).Q(null, ToolkitKitRow.IconClass).style.backgroundImage.value.texture;

            Assert.IsNotNull(icon, "No icon source falls through to the procedural icon.");
        }

        [Test]
        public void PointerEnter_Chip_AsksThePopoverForTheEntryDetails()
        {
            _row.Refresh(new[] { Entry("Heal") });

            Send(_row.ChipAt(0), PointerEnterEvent.GetPooled());

            Assert.AreEqual(1, _requested.Count);
            Assert.AreEqual("Heal", _requested[0].title);
            Assert.AreEqual("Heal details", _requested[0].description);
            Assert.IsNotNull(_requested[0].iconSource);
        }

        [Test]
        public void PointerLeave_Chip_ClosesThePopover()
        {
            _row.Refresh(new[] { Entry("Heal") });

            Send(_row.ChipAt(0), PointerLeaveEvent.GetPooled());

            Assert.AreEqual(1, _closed);
        }

        [Test]
        public void PointerDown_Chip_StopsBeforeTheCardButton()
        {
            _row.Refresh(new[] { Entry("Heal") });
            int reached = 0;
            _host.RegisterCallback<PointerDownEvent>(evt => reached++);

            Send(_row.ChipAt(0), PointerDownEvent.GetPooled());

            Assert.AreEqual(0, reached, "A tap on an icon must not reach the card's button.");
            Assert.AreEqual(1, _requested.Count, "It opens the details instead.");
        }

        [Test]
        public void Dispose_Row_RemovesTheChips()
        {
            _row.Refresh(new[] { Entry("Heal") });

            _row.Dispose();

            Assert.AreEqual(0, _row.count);
            Assert.AreEqual(0, _host.childCount);
        }
    }
}
