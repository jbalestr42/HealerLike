using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitEncounterBarTests
    {
        ToolkitGameView _view;

        [SetUp]
        public void SetUp()
        {
            _view = new ToolkitGameView(Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree());
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
        }

        [Test]
        public void Template_SandboxButton_SitsBesideStartAndStartsHidden()
        {
            Button start = _view.root.Q<Button>("start-button");
            Button sandbox = _view.root.Q<Button>("sandbox-button");

            Assert.IsNotNull(sandbox);
            Assert.AreSame(start.parent, sandbox.parent);
            Assert.IsTrue(sandbox.ClassListContains("is-hidden"), "Only the menu offers the sandbox.");
        }

        [Test]
        public void RefreshMenu_ShowsAnEnabledSandboxEntryBesideStart()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext { isMenu = true }, _view);

            bar.RefreshMenu();

            Button sandbox = _view.root.Q<Button>("sandbox-button");
            Assert.IsFalse(sandbox.ClassListContains("is-hidden"));
            Assert.IsTrue(sandbox.enabledSelf);
            Assert.AreEqual("Sandbox", sandbox.text);
            Assert.IsFalse(_view.root.Q<Button>("start-button").ClassListContains("is-hidden"));
        }

        [Test]
        public void RefreshMenu_NoPopover_HidesTheDetailPanel()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext { isMenu = true }, _view);
            _view.Show("detail-panel", true);

            bar.RefreshMenu();

            Assert.IsTrue(_view.root.Q("detail-panel").ClassListContains("is-hidden"));
        }

        // A class card's kit chip opens the popover on the menu, and the menu's refresh keeps it up
        [Test]
        public void RefreshMenu_PopoverOpen_KeepsTheDetailPanelShown()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext { isMenu = true }, _view);
            _view.Show("detail-panel", true);

            bar.RefreshMenu(true);

            Assert.IsFalse(_view.root.Q("detail-panel").ClassListContains("is-hidden"));
        }
    }
}
