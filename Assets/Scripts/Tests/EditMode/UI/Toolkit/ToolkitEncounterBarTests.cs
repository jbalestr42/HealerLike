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

        [Test]
        public void Template_ManaGauge_HoldsItsFillAndTheValueAboveTheSpells()
        {
            VisualElement gauge = _view.root.Q("mana-gauge");

            Assert.IsNotNull(gauge);
            Assert.AreSame(gauge, _view.root.Q("mana-fill").parent);
            Assert.AreSame(gauge, _view.root.Q<Label>("mana-value").parent);
            Assert.AreSame(gauge.parent, _view.root.Q("spell-list").parent);
            Assert.Less(gauge.parent.IndexOf(gauge), gauge.parent.IndexOf(_view.root.Q("spell-list")));
        }

        [TestCase(50f, 100f, 0.5f)]
        [TestCase(100f, 100f, 1f)]
        [TestCase(0f, 100f, 0f)]
        public void ManaFraction_IsTheShareOfTheMaximum(float value, float max, float expected)
        {
            Assert.AreEqual(expected, ToolkitEncounterBar.ManaFraction(value, max), 0.0001f);
        }

        [Test]
        public void ManaFraction_AboveTheMaximumOrWithoutOne_StaysInTheGauge()
        {
            Assert.AreEqual(1f, ToolkitEncounterBar.ManaFraction(150f, 100f), 0.0001f);
            Assert.AreEqual(0f, ToolkitEncounterBar.ManaFraction(-5f, 100f), 0.0001f);
            Assert.AreEqual(0f, ToolkitEncounterBar.ManaFraction(20f, 0f), 0.0001f);
        }
    }
}
