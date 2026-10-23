using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UI.Toolkit
{

    // The reward screen shows one card per reward choice of the run, whatever the kind of the choice
    public class ToolkitRewardPanelTests
    {
        ToolkitTestRun _run;
        VisualElement _root;
        ToolkitGameView _view;
        ToolkitRewardPanel _panel;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _run = new ToolkitTestRun();
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(_root);
            _panel = new ToolkitRewardPanel();
            _panel.Init(null, _run.context, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
            _run.Dispose();
        }

        public static EntityData CreateUnit(string title, List<Object> owned)
        {
            EntityData unit = ScriptableObject.CreateInstance<EntityData>();
            owned.Add(unit);
            unit.title = title;
            unit.description = "Hits harder when healthy";
            unit.attributes = new Dictionary<AttributeType, float> { { AttributeType.HealthMax, 100f } };
            unit.items = new List<AItemFactory>();
            return unit;
        }

        public static Item CreateItem(string name)
        {
            return new Item { data = new ItemData { name = name, description = $"{name} description" } };
        }

        RewardChoice UnitChoice(string title)
        {
            return new RewardChoice { type = RewardChoiceType.Unit, unit = CreateUnit(title, _objects) };
        }

        static RewardChoice ItemChoice(RewardChoiceType type, string name)
        {
            return new RewardChoice { type = type, item = CreateItem(name) };
        }

        List<VisualElement> Cards()
        {
            return _root.Q("upgrade-list").Query(className: "data-card").ToList();
        }

        List<string> CardTitles()
        {
            return _root.Q("upgrade-list").Query<Label>("card-title").ToList().Select(label => label.text).ToList();
        }

        static IEnumerable<RewardChoiceType> Kinds()
        {
            foreach (RewardChoiceType kind in Enum.GetValues(typeof(RewardChoiceType)))
            {
                yield return kind;
            }
        }

        // A kind added to the rewards and unknown to the panel fails here by name
        [TestCaseSource(nameof(Kinds))]
        public void EveryRewardKind_BecomesACardThatCanBeChosen(RewardChoiceType kind)
        {
            RewardChoice choice = kind == RewardChoiceType.Unit ? UnitChoice("Zealot") : ItemChoice(kind, "Ward");

            List<ToolkitCardModel> models = _panel.BuildModels(new[] { choice });

            Assert.AreEqual(1, models.Count, $"A {kind} choice produced no card.");
            Assert.IsNotEmpty(models[0].title, $"A {kind} card has no title.");
            Assert.IsNotNull(models[0].iconSource, $"A {kind} card has nothing to draw an icon from.");
            Assert.IsNotNull(models[0].activate, $"A {kind} card cannot be chosen.");
            Assert.AreSame(choice, models[0].source);
        }

        [Test]
        public void UnitChoice_IsACardWithItsTitleDetailsAndIcon()
        {
            RewardChoice choice = UnitChoice("Zealot");

            ToolkitCardModel model = _panel.BuildModels(new[] { choice })[0];

            Assert.AreEqual("New unit: Zealot", model.title);
            Assert.AreEqual(CharacterCardText.GetUnitDetails(choice.unit), model.description);
            Assert.AreSame(choice.unit, model.iconSource, "The icon service resolves the unit from its data.");
            StringAssert.EndsWith("· Choose reward", model.status);
            StringAssert.Contains("unit", model.status.ToLowerInvariant());
        }

        [Test]
        public void ItemChoices_TellThePartyEquipmentFromTheHealerUpgrade()
        {
            List<ToolkitCardModel> models = _panel.BuildModels(new[]
            {
                ItemChoice(RewardChoiceType.EntityItem, "Ward"),
                ItemChoice(RewardChoiceType.PlayerItem, "Bloom"),
            });

            Assert.AreEqual("Ward description", models[0].description);
            StringAssert.StartsWith("Party equipment", models[0].status);
            StringAssert.StartsWith("Healer upgrade", models[1].status);
        }

        [Test]
        public void AuthoredRewardUnits_EachBecomeACard()
        {
            List<EntityData> rewardUnits = UnityEditor.AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Data" })
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<EntityData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(unit => unit != null && unit.tags != null && unit.tags.Any(tag => tag != null && tag.name == TagNames.Reward))
                .ToList();
            Assert.IsNotEmpty(rewardUnits, "The game data offers units as rewards.");

            List<ToolkitCardModel> models = _panel.BuildModels(
                rewardUnits.Select(unit => new RewardChoice { type = RewardChoiceType.Unit, unit = unit }).ToList());

            CollectionAssert.AreEqual(rewardUnits.Select(unit => $"New unit: {unit.title}").ToList(),
                models.Select(model => model.title).ToList());
        }

        [Test]
        public void ChoiceWithoutItsUnitOrItem_IsSkipped()
        {
            List<ToolkitCardModel> models = _panel.BuildModels(new[]
            {
                new RewardChoice { type = RewardChoiceType.Unit },
                new RewardChoice { type = RewardChoiceType.EntityItem },
                ItemChoice(RewardChoiceType.EntityItem, "Ward"),
            });

            CollectionAssert.AreEqual(new[] { "Ward" }, models.Select(model => model.title).ToList());
        }

        [Test]
        public void Refresh_RewardScreen_DrawsTheChoicesInOrder()
        {
            _run.ShowRewards(
                ItemChoice(RewardChoiceType.EntityItem, "Ward"),
                UnitChoice("Zealot"),
                ItemChoice(RewardChoiceType.PlayerItem, "Bloom"));

            _panel.Refresh();

            Assert.AreEqual(3, Cards().Count);
            CollectionAssert.AreEqual(new[] { "Ward", "New unit: Zealot", "Bloom" }, CardTitles());
        }

        [Test]
        public void Activate_PartyEquipment_GoesToTheStashAndClosesTheRewards()
        {
            RewardChoice ward = ItemChoice(RewardChoiceType.EntityItem, "Ward");
            _run.ShowRewards(ward, ItemChoice(RewardChoiceType.EntityItem, "Bloom"));
            ToolkitCardModel model = _panel.BuildModels(_run.ascension.rewardChoices)[0];

            model.activate.Invoke(model);

            CollectionAssert.AreEqual(new[] { ward.item }, _run.player.stash.items.Select(entry => entry.item).ToList());
            Assert.AreEqual(GameScreen.Game, _run.game.screens.current);
            Assert.IsEmpty(_run.ascension.rewardChoices);
        }

        [Test]
        public void Refresh_OffTheRewardScreen_DrawsNothing()
        {
            _panel.Refresh();

            Assert.IsEmpty(Cards());
        }
    }
}
