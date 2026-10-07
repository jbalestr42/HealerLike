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
            VisualElement root = new VisualElement();
            VisualElement list = new VisualElement { name = "spell-list" };
            Button card = new Button { userData = new ToolkitCardModel { title = "Heal" } };
            card.AddToClassList("data-card");
            VisualElement icon = new VisualElement { name = "card-icon" };
            Label title = new Label("Heal") { name = "card-title" };
            root.Add(list);
            list.Add(card);
            card.Add(icon);
            card.Add(title);
            Texture2D first = new Texture2D(4, 4);
            Texture2D next = new Texture2D(4, 4);
            StageIconLabels labels = new StageIconLabels();
            try
            {
                icon.style.backgroundImage = first;
                labels.Update(root);
                VisualElement art = icon.Q("render-card-art");
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
