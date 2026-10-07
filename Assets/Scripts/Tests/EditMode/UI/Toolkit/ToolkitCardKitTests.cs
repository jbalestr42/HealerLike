using NUnit.Framework;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The class card's stats block and kit row: shown when the model has them, hidden again when a card is reused
    public class ToolkitCardKitTests
    {
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        VisualElement _list;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _list = new VisualElement();
            _list.name = "class-list";
            _panel.root.Add(_list);
            _view = new ToolkitGameView(_panel.root);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            _panel.Dispose();
        }

        [Test]
        public void Refresh_ModelWithStatsAndKit_ShowsThem()
        {
            ToolkitKitEntry heal = new ToolkitKitEntry { iconSource = new ToolkitClassSelect.RandomChoice(), title = "Heal", body = "" };

            _view.SetCards("class-list", new[]
            {
                new ToolkitCardModel { title = "Cleric", stats = "- Heal Power: 30", kit = new[] { heal } }
            });

            Label stats = _list.Q<Label>("card-stats");
            Assert.AreEqual("- Heal Power: 30", stats.text);
            Assert.AreEqual(DisplayStyle.Flex, stats.style.display.value);
            Assert.AreEqual(1, _list.Q("card-kit").childCount);
            Assert.AreEqual(DisplayStyle.Flex, _list.Q("card-kit").style.display.value);
        }

        [Test]
        public void Refresh_ReusedCardWithoutStatsOrKit_HidesTheOldOnes()
        {
            ToolkitKitEntry heal = new ToolkitKitEntry { iconSource = new ToolkitClassSelect.RandomChoice(), title = "Heal", body = "" };
            _view.SetCards("class-list", new[] { new ToolkitCardModel { title = "Cleric", stats = "- Heal Power: 30", kit = new[] { heal } } });

            _view.SetCards("class-list", new[] { new ToolkitCardModel { title = "Random" } });

            Assert.AreEqual("", _list.Q<Label>("card-stats").text);
            Assert.AreEqual(DisplayStyle.None, _list.Q<Label>("card-stats").style.display.value);
            Assert.AreEqual(0, _list.Q("card-kit").childCount);
            Assert.AreEqual(DisplayStyle.None, _list.Q("card-kit").style.display.value);
        }

        [Test]
        public void Refresh_CardWithoutKit_LeavesNoKitDisplayed()
        {
            _view.SetCards("class-list", new[] { new ToolkitCardModel { title = "Random" } });

            Assert.AreEqual(DisplayStyle.None, _list.Q("card-kit").style.display.value);
        }
    }
}
