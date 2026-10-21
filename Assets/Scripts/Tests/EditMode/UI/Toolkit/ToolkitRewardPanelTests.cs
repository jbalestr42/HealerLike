using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    // The reward screen shows one card per choice of Julien's upgrade view, whatever the kind of the choice
    public class ToolkitRewardPanelTests
    {
        GameObject _host;
        UpgradeView _upgradeView;
        List<GameObject> _choices;
        VisualElement _root;
        ToolkitGameView _view;
        ToolkitRewardPanel _panel;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Reward adapter fixture");
            _host.SetActive(false);
            ToolkitGameContext context = new ToolkitGameContext();
            context.ui = _host.AddComponent<UIManager>();
            GameObject viewHost = new GameObject("Legacy upgrade view", typeof(RectTransform), typeof(CanvasGroup));
            viewHost.transform.SetParent(_host.transform);
            _upgradeView = viewHost.AddComponent<UpgradeView>();
            _choices = new List<GameObject>();
            TestHelpers.SetPrivateField(_upgradeView, "_upgradeButtons", _choices);
            TestHelpers.SetPrivateField(
                context.ui, "_views", new Dictionary<ViewType, AView> { { ViewType.Upgrade, _upgradeView } });
            TestHelpers.SetPrivateField(context.ui, "_currentView", ViewType.Upgrade);
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(_root);
            _panel = new ToolkitRewardPanel();
            _panel.Init(null, context, _view);
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
            Object.DestroyImmediate(_host);
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

        // A choice the way Julien's view builds it: a button object carrying the component of its kind
        public static GameObject CreateChoice<T>(string fieldName, object value, List<Object> owned) where T : Component
        {
            GameObject choice = new GameObject(typeof(T).Name, typeof(RectTransform));
            choice.SetActive(false);
            owned.Add(choice);
            T button = choice.AddComponent<T>();
            TestHelpers.SetPrivateField(button, fieldName, value);
            return choice;
        }

        public static Item CreateItem(string name)
        {
            return new Item { data = new ItemData { name = name, description = $"{name} description" } };
        }

        List<VisualElement> Cards()
        {
            return _root.Q("upgrade-list").Query(className: "data-card").ToList();
        }

        List<string> CardTitles()
        {
            return _root.Q("upgrade-list").Query<Label>("card-title").ToList().Select(label => label.text).ToList();
        }

        [Test]
        public void UnitChoice_IsACardWithItsTitleDetailsAndIcon()
        {
            EntityData zealot = CreateUnit("Zealot", _objects);
            GameObject choice = CreateChoice<SelectEntityUpgradeButton>("_entity", zealot, _objects);

            List<ToolkitCardModel> models = _panel.BuildModels(new[] { choice });

            Assert.AreEqual(1, models.Count, "A unit reward must not be skipped.");
            ToolkitCardModel model = models[0];
            Assert.AreEqual(SelectEntityUpgradeButton.GetTitle(zealot), model.title);
            Assert.AreEqual("New unit: Zealot", model.title);
            Assert.AreEqual(CharacterCardText.GetUnitDetails(zealot), model.description);
            Assert.AreSame(zealot, model.iconSource, "The icon service resolves the unit from its data.");
            Assert.AreSame(choice.GetComponent<SelectEntityUpgradeButton>(), model.source,
                "Activating the card dispatches Julien's own unit selection.");
            Assert.IsNotNull(model.activate);
            StringAssert.EndsWith("· Choose reward", model.status);
            StringAssert.Contains("unit", model.status.ToLowerInvariant());
        }

        [Test]
        public void RewardOnlyUnit_OutsideTheCharacterRoster_IsACardLikeAnyUnit()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            _objects.Add(character);
            character.entities = new List<EntityData> { CreateUnit("Cleric", _objects) };
            GameplayTag reward = ScriptableObject.CreateInstance<GameplayTag>();
            _objects.Add(reward);
            reward.name = TagNames.Reward;
            EntityData hermit = CreateUnit("Hermit", _objects);
            hermit.tags = new List<GameplayTag> { reward };
            Assert.IsFalse(character.entities.Contains(hermit));
            GameObject choice = CreateChoice<SelectEntityUpgradeButton>("_entity", hermit, _objects);

            List<ToolkitCardModel> models = _panel.BuildModels(new[] { choice });

            Assert.AreEqual(1, models.Count);
            Assert.AreEqual("New unit: Hermit", models[0].title);
            Assert.AreSame(hermit, models[0].iconSource);
        }

        [Test]
        public void AuthoredRewardUnits_EachBecomeACard()
        {
            List<EntityData> rewardUnits = UnityEditor.AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Data" })
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<EntityData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(unit => unit != null && unit.tags != null && unit.tags.Any(tag => tag != null && tag.name == TagNames.Reward))
                .ToList();
            Assert.IsNotEmpty(rewardUnits, "The game data offers units as rewards.");

            List<GameObject> choices = rewardUnits
                .Select(unit => CreateChoice<SelectEntityUpgradeButton>("_entity", unit, _objects))
                .ToList();
            List<ToolkitCardModel> models = _panel.BuildModels(choices);

            CollectionAssert.AreEqual(rewardUnits.Select(SelectEntityUpgradeButton.GetTitle).ToList(),
                models.Select(model => model.title).ToList());
        }

        [Test]
        public void MixedChoices_AllThreeKindsAreDrawnInOrder()
        {
            _choices.Add(CreateChoice<SelectItemUpgradeButton>("_item", CreateItem("Ward"), _objects));
            _choices.Add(CreateChoice<SelectEntityUpgradeButton>("_entity", CreateUnit("Zealot", _objects), _objects));
            _choices.Add(CreateChoice<SelectPlayerItemUpgradeButton>("_item", CreateItem("Bloom"), _objects));

            _panel.Refresh();

            Assert.AreEqual(3, Cards().Count);
            CollectionAssert.AreEqual(new[] { "Ward", "New unit: Zealot", "Bloom" }, CardTitles());
        }

        [Test]
        public void UnitChoiceWithoutUnit_IsSkippedWithoutThrowing()
        {
            _choices.Add(CreateChoice<SelectEntityUpgradeButton>("_entity", null, _objects));
            _choices.Add(CreateChoice<SelectItemUpgradeButton>("_item", CreateItem("Ward"), _objects));

            _panel.Refresh();

            CollectionAssert.AreEqual(new[] { "Ward" }, CardTitles());
        }

        [Test]
        public void ChoicesWithNoCard_LogAnErrorNamingTheirComponents()
        {
            GameObject unknown = new GameObject("Unknown reward", typeof(RectTransform));
            unknown.SetActive(false);
            _objects.Add(unknown);
            unknown.AddComponent<CanvasGroup>();
            unknown.AddComponent<UnityEngine.UI.Image>();
            _choices.Add(unknown);
            LogAssert.Expect(LogType.Error, new Regex(@"ToolkitRewardPanel.*1 choice.*Unknown reward \[Image\]"));

            _panel.Refresh();

            Assert.IsEmpty(Cards());
        }

        [Test]
        public void ChoicesWithNoCard_AreReportedOncePerScreen()
        {
            _choices.Add(CreateChoice<SelectItemUpgradeButton>("_item", null, _objects));
            _choices.Add(CreateChoice<SelectPlayerItemUpgradeButton>("_item", null, _objects));
            LogAssert.Expect(LogType.Error, new Regex("2 choice.*SelectItemUpgradeButton.*SelectPlayerItemUpgradeButton"));

            _panel.Refresh();
            _panel.Refresh();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EmptyChoiceList_IsNotAnError()
        {
            _panel.Refresh();

            Assert.IsEmpty(Cards());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SomeChoicesDrawn_IsNotAnError()
        {
            _choices.Add(CreateChoice<SelectItemUpgradeButton>("_item", null, _objects));
            _choices.Add(CreateChoice<SelectItemUpgradeButton>("_item", CreateItem("Ward"), _objects));

            _panel.Refresh();

            CollectionAssert.AreEqual(new[] { "Ward" }, CardTitles());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ReaderUnit_NullButton_IsNullWithoutLogging()
        {
            Assert.IsNull(LegacyUiReader.Unit(null));
        }

        [Test]
        public void ReaderUnit_ReadsTheCurrentUnitOfTheButton()
        {
            EntityData zealot = CreateUnit("Zealot", _objects);
            GameObject choice = CreateChoice<SelectEntityUpgradeButton>("_entity", zealot, _objects);

            Assert.AreSame(zealot, LegacyUiReader.Unit(choice.GetComponent<SelectEntityUpgradeButton>()));
        }
    }
}
