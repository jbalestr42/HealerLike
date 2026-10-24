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

        // In battle the toolbar has nothing to show, but keeps its place: the field under it must not move
        [Test]
        public void Refresh_InBattleWithNoAction_KeepsTheToolbarsPlaceButHidesIt()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext(), _view);

            bar.Refresh(false, false, false);

            VisualElement toolbar = _view.root.Q("field-toolbar");
            Assert.IsFalse(toolbar.ClassListContains("is-hidden"));
            Assert.IsTrue(toolbar.ClassListContains(ToolkitEncounterBar.EmptyToolbarClass));
        }

        // A spell being aimed shows Cancel: the toolbar becomes visible without changing its layout
        [Test]
        public void Refresh_InBattleWithCancelShown_ShowsTheToolbarInTheSamePlace()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext(), _view);
            _view.Show("cancel-button", true);

            bar.Refresh(false, false, false);

            VisualElement toolbar = _view.root.Q("field-toolbar");
            Assert.IsFalse(toolbar.ClassListContains("is-hidden"));
            Assert.IsFalse(toolbar.ClassListContains(ToolkitEncounterBar.EmptyToolbarClass));
        }

        [Test]
        public void Refresh_Preparing_ShowsTheStartBattleButton()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext(), _view);

            bar.Refresh(false, true, false);

            Assert.IsFalse(_view.root.Q("field-toolbar").ClassListContains(ToolkitEncounterBar.EmptyToolbarClass));
            Assert.IsFalse(_view.root.Q("wave-button").ClassListContains("is-hidden"));
        }

        [Test]
        public void RefreshMenu_AfterAnEmptyBattleToolbar_ShowsItsButtons()
        {
            ToolkitEncounterBar bar = new ToolkitEncounterBar();
            bar.Init(new ToolkitGameContext(), _view);
            bar.Refresh(false, false, false);

            bar.RefreshMenu();

            Assert.IsFalse(_view.root.Q("field-toolbar").ClassListContains(ToolkitEncounterBar.EmptyToolbarClass));
        }

        [Test]
        public void HasVisibleAction_NoActionsOrAllHidden_IsFalse()
        {
            VisualElement actions = new VisualElement();
            VisualElement hidden = new Button();
            hidden.AddToClassList("is-hidden");
            actions.Add(hidden);

            Assert.IsFalse(ToolkitEncounterBar.HasVisibleAction(null));
            Assert.IsFalse(ToolkitEncounterBar.HasVisibleAction(actions));

            actions.Add(new Button());
            Assert.IsTrue(ToolkitEncounterBar.HasVisibleAction(actions));
        }

        [Test]
        public void Template_ManaGauge_HoldsItsFillAndTheValueBeforeTheDevotion()
        {
            VisualElement gauge = _view.root.Q("mana-gauge");
            VisualElement devotion = _view.root.Q(ToolkitHudStatus.DevotionName);

            Assert.IsNotNull(gauge);
            Assert.AreSame(gauge, _view.root.Q("mana-fill").parent);
            Assert.AreSame(gauge, _view.root.Q<Label>("mana-value").parent);
            Assert.AreSame(gauge.parent, devotion.parent);
            Assert.Less(gauge.parent.IndexOf(gauge), gauge.parent.IndexOf(devotion));
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
