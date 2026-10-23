using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    // The choices of an event or rest room become Toolkit cards, a pick goes to the run
    public class ToolkitEventPanelTests
    {
        ToolkitTestRun _run;
        VisualElement _root;
        ToolkitGameView _view;
        ToolkitEventPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _run = new ToolkitTestRun();
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(_root);
            _panel = new ToolkitEventPanel();
            _panel.Init(null, _run.context, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            _run.Dispose();
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
            EventChoice heal = Choice("Heal", description: "Every unit recovers 30% of its max health.");

            List<ToolkitCardModel> models = _panel.BuildModels(new[] { heal, Choice("Resurrect") });

            Assert.AreEqual(2, models.Count);
            Assert.AreEqual("Heal", models[0].title);
            Assert.AreEqual("Every unit recovers 30% of its max health.", models[0].description);
            Assert.IsTrue(models[0].isEnabled);
            Assert.AreSame(heal, models[0].source);
            Assert.IsNotNull(models[0].activate);
            Assert.AreEqual("Resurrect", models[1].title);
        }

        [Test]
        public void BuildModels_UnavailableChoice_IsADisabledCardWithAnUnavailableStatus()
        {
            List<ToolkitCardModel> models = _panel.BuildModels(new[] { Choice("Heal"), Choice("Resurrect", isAvailable: false) });

            Assert.IsFalse(models[1].isEnabled);
            Assert.AreEqual("Unavailable", models[1].status);
        }

        [Test]
        public void BuildModels_ChoiceWithoutDescription_HasAnEmptyDescription()
        {
            List<ToolkitCardModel> models = _panel.BuildModels(new[] { new EventChoice { label = "Back" } });

            Assert.AreEqual(string.Empty, models[0].description);
        }

        [Test]
        public void BuildModels_ChoiceGivingAUnit_ShowsTheUnit()
        {
            EntityData unit = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                EventChoice recruit = Choice("Zealot");
                recruit.source = unit;

                List<ToolkitCardModel> models = _panel.BuildModels(new[] { recruit, Choice("Leave") });

                Assert.AreSame(unit, models[0].iconSource);
                Assert.AreEqual("Unit · Choose", models[0].status);
                Assert.IsInstanceOf<ToolkitEventPanel.ChoiceIcon>(models[1].iconSource);
            }
            finally
            {
                Object.DestroyImmediate(unit);
            }
        }

        [Test]
        public void BuildModels_NoAsset_UsesAProceduralIconLabelledWithTheChoice()
        {
            ToolkitCardModel model = _panel.BuildModels(new[] { Choice("Leave") })[0];

            IDataIconMetadata icon = model.iconSource as IDataIconMetadata;
            Assert.IsNotNull(icon);
            Assert.AreEqual("Leave", icon.label);
            Assert.IsNotNull(_view.GetIcon(model.iconSource, out bool isPortrait));
            Assert.IsFalse(isPortrait);
        }

        [Test]
        public void Activate_Card_ClosesTheEventThenAppliesTheChoiceOnce()
        {
            int picked = 0;
            _run.ShowEvent("Rest", "Take a break", Choice("Heal", () => picked++));
            _panel.Refresh();
            ToolkitCardModel model = _panel.BuildModels(_run.ascension.currentEvent.choices)[0];

            model.activate.Invoke(model);
            model.activate.Invoke(model);

            Assert.AreEqual(1, picked);
            Assert.AreEqual(GameScreen.Game, _run.game.screens.current);
            Assert.IsNull(_run.ascension.currentEvent);
        }

        [Test]
        public void Activate_UnavailableChoice_AppliesNothing()
        {
            int picked = 0;
            _run.ShowEvent("Rest", "", Choice("Resurrect", () => picked++, isAvailable: false));
            ToolkitCardModel model = _panel.BuildModels(_run.ascension.currentEvent.choices)[0];

            model.activate.Invoke(model);

            Assert.AreEqual(0, picked);
            Assert.AreEqual(GameScreen.Event, _run.game.screens.current);
        }

        [Test]
        public void Refresh_EventOnScreen_DrawsItsTitleDescriptionAndCards()
        {
            _run.ShowEvent("Rest", "Take a break", Choice("Heal"), Choice("Resurrect", isAvailable: false));

            _panel.Refresh();

            Assert.IsFalse(IsPanelHidden());
            Assert.AreEqual("Rest", _root.Q<Label>("event-title").text);
            Assert.AreEqual("Take a break", _root.Q<Label>("event-description").text);
            CollectionAssert.AreEqual(new[] { "Heal", "Resurrect" }, CardTitles());
        }

        [Test]
        public void Refresh_EventWithoutTitleNorDescription_ShowsTheDefaultTitleAndHidesTheDescription()
        {
            _run.ShowEvent("", "", Choice("Leave"));

            _panel.Refresh();

            Assert.AreEqual(ToolkitEventPanel.DefaultTitle, _root.Q<Label>("event-title").text);
            Assert.IsTrue(_root.Q("event-description").ClassListContains("is-hidden"));
        }

        [Test]
        public void Refresh_NextEventOfTheRoom_ReplacesTheCards()
        {
            _run.ShowEvent("Rest", "", Choice("Heal"), Choice("Resurrect"));
            _panel.Refresh();

            // Resurrect opens the choice of the unit
            _run.ShowEvent("Resurrect", "Choose the unit to bring back.", Choice("Zealot"), Choice("Back"));
            _panel.Refresh();

            CollectionAssert.AreEqual(new[] { "Zealot", "Back" }, CardTitles());
        }

        [Test]
        public void Refresh_OffTheEventScreen_HidesThePanel()
        {
            _run.ShowEvent("Rest", "", Choice("Heal", () => { }));
            _panel.Refresh();
            _run.ascension.SelectEventChoice(_run.ascension.currentEvent.choices[0]);

            _panel.Refresh();

            Assert.IsTrue(IsPanelHidden());
        }
    }
}
