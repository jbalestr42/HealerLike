using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageIconLabelsTests
    {
        [Test]
        public void LayoutPreservesGrammarArtworkAndFollowsProviderRefresh()
        {
            var root = new VisualElement();
            var list = new VisualElement { name = "spell-list" };
            var card = new Button { userData = new ToolkitCardModel { title = "Heal" } };
            card.AddToClassList("data-card");
            var icon = new VisualElement { name = "card-icon" };
            var title = new Label("Heal") { name = "card-title" };
            root.Add(list);
            list.Add(card);
            card.Add(icon);
            card.Add(title);
            var first = new Texture2D(4, 4);
            var next = new Texture2D(4, 4);
            var labels = new StageIconLabels();
            try
            {
                icon.style.backgroundImage = first;
                labels.Update(root);
                var art = icon.Q("render-card-art");
                Assert.AreSame(first, art.style.backgroundImage.value.texture);
                Assert.AreSame(icon, title.parent);
                labels.Update(root);
                Assert.AreSame(first, art.style.backgroundImage.value.texture);
                icon.style.backgroundImage = next;
                labels.Update(root);
                Assert.AreSame(next, art.style.backgroundImage.value.texture);
                labels.Dispose();
                Assert.IsTrue(first && next, "Artwork stays owned by the icon provider.");
            }
            finally
            {
                labels.Dispose();
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(next);
            }
        }
    }
}
