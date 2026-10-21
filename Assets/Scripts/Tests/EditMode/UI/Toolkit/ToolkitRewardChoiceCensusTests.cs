using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UI.Toolkit
{

    // Every kind of reward choice Julien's UpgradeView can offer becomes a Toolkit card. A kind he adds has no
    // builder below and fails here by name, before a run can lose that choice off screen
    public class ToolkitRewardChoiceCensusTests
    {
        readonly List<Object> _objects = new List<Object>();
        ToolkitGameView _view;

        [TearDown]
        public void TearDown()
        {
            if (_view != null)
            {
                _view.Release();
                _view = null;
            }

            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        static IEnumerable<RewardChoiceType> Kinds()
        {
            foreach (RewardChoiceType kind in Enum.GetValues(typeof(RewardChoiceType)))
            {
                yield return kind;
            }
        }

        // The choice his FillChoices instantiates for a kind, carrying that kind's button component. A new kind
        // needs a line here, and a branch in ToolkitRewardPanel for its button
        GameObject CreateChoice(RewardChoiceType kind)
        {
            switch (kind)
            {
                case RewardChoiceType.Unit:
                    return ToolkitRewardPanelTests.CreateChoice<SelectEntityUpgradeButton>(
                        "_entity", ToolkitRewardPanelTests.CreateUnit("Zealot", _objects), _objects);
                case RewardChoiceType.PlayerItem:
                    return ToolkitRewardPanelTests.CreateChoice<SelectPlayerItemUpgradeButton>(
                        "_item", ToolkitRewardPanelTests.CreateItem("Bloom"), _objects);
                case RewardChoiceType.EntityItem:
                    return ToolkitRewardPanelTests.CreateChoice<SelectItemUpgradeButton>(
                        "_item", ToolkitRewardPanelTests.CreateItem("Ward"), _objects);
                default:
                    Assert.Fail($"RewardChoiceType.{kind} is a reward kind the Toolkit reward panel has never seen: "
                        + "add its button to ToolkitRewardPanel and a builder to ToolkitRewardChoiceCensusTests.");
                    return null;
            }
        }

        [Test]
        public void Census_CoversTheThreeKindsKnownToday()
        {
            // Not the gate, the cases below are: this only proves the enum is read and the census is not empty
            CollectionAssert.IsSupersetOf(Kinds(), new[]
            {
                RewardChoiceType.Unit, RewardChoiceType.PlayerItem, RewardChoiceType.EntityItem,
            });
        }

        [TestCaseSource(nameof(Kinds))]
        public void EveryRewardKind_BecomesACardThatActivatesItsChoice(RewardChoiceType kind)
        {
            GameObject choice = CreateChoice(kind);
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(root);
            ToolkitRewardPanel panel = new ToolkitRewardPanel();
            panel.Init(null, new ToolkitGameContext(), _view);

            List<ToolkitCardModel> models = panel.BuildModels(new[] { choice });

            Assert.AreEqual(1, models.Count, $"A {kind} choice produced no card.");
            Assert.IsNotEmpty(models[0].title, $"A {kind} card has no title.");
            Assert.IsNotNull(models[0].iconSource, $"A {kind} card has nothing to draw an icon from.");
            Assert.IsNotNull(models[0].activate, $"A {kind} card cannot be chosen.");
            Assert.IsInstanceOf<MonoBehaviour>(models[0].source, $"A {kind} card does not act on Julien's button.");
            Assert.AreSame(choice, ((MonoBehaviour)models[0].source).gameObject);
            _view.SetCards("upgrade-list", models);
            Assert.AreEqual(1, root.Q("upgrade-list").Query(className: "data-card").ToList().Count,
                $"A {kind} card is not drawn in the reward list.");
        }
    }
}
