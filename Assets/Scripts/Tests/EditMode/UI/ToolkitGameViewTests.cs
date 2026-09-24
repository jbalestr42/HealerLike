using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.UI.Toolkit.Tests
{
    public class ToolkitGameViewTests
    {
        [Test]
        public void CardRefreshReusesVisualsAndRemovesStaleEntries()
        {
            var root = new VisualElement();
            var list = new VisualElement { name = "cards" };
            root.Add(list);
            var view = new ToolkitGameView(root);
            view.Cards("cards", new[] { new ToolkitCardModel { Title = "Heal" }, new ToolkitCardModel { Title = "Guard" } });
            Assert.That(list.childCount, Is.EqualTo(2));
            var first = list[0];
            view.Cards("cards", new[] { new ToolkitCardModel { Title = "New spell", Enabled = false } });
            Assert.That(list.childCount, Is.EqualTo(1));
            Assert.That(list[0], Is.SameAs(first));
            Assert.That(first.enabledSelf, Is.False);
            Assert.That(first.Q<Label>(className: "data-card__title").text, Is.EqualTo("New spell"));
        }

        [Test]
        public void MissingIconSourceStillReceivesAutomaticFallback()
        {
            var root = new VisualElement();
            var list = new VisualElement { name = "cards" };
            root.Add(list);
            var view = new ToolkitGameView(root);
            view.Cards("cards", new[] { new ToolkitCardModel { Title = "Unknown data" } });
            Assert.That(list[0].Q(className: "data-card__icon").style.backgroundImage.value.texture, Is.Not.Null);
        }

        [Test]
        public void VisibilityAndProgressUseSemanticElements()
        {
            var root = new VisualElement();
            var panel = new VisualElement { name = "inventory-panel" };
            var bar = new ProgressBar { name = "mana-bar" };
            root.Add(panel);
            root.Add(bar);
            var view = new ToolkitGameView(root);
            view.Visible("inventory-panel", false);
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            view.Visible("inventory-panel", true);
            Assert.That(panel.ClassListContains("is-hidden"), Is.False);
            view.Resource("mana-bar", 30f, 60f);
            Assert.That(bar.value, Is.EqualTo(50f));
        }

        [Test]
        public void RuntimeLayoutAndCardTemplatesResolveFromResources()
        {
            Assert.That(Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI"), Is.Not.Null);
            var cards = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
            Assert.That(cards, Is.Not.Null);
            var tree = cards.CloneTree();
            Assert.That(tree.Q<Button>("data-card"), Is.Not.Null);
            Assert.That(tree.Q("card-icon"), Is.Not.Null);
        }
    }
}
