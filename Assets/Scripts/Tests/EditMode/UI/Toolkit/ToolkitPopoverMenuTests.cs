using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The popover over the menu's class screen: above the modal, and placed with the header and party panel hidden
    public class ToolkitPopoverMenuTests
    {
        [UnityTest]
        public IEnumerator Open_MenuWithHiddenHeaderAndParty_StaysInsideTheScreenAboveTheModal()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                panel.root.Add(root);
                ToolkitTheme.Apply(root, null);
                ToolkitGameView view = new ToolkitGameView(root);
                view.Show("top-bar", false);
                view.Show("party-panel", false);
                view.Show("class-panel", true);
                root.style.width = 844;
                root.style.height = 390;
                yield return null;
                yield return null;
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    view.inspectAnchor = new Rect(400, 200, 40, 40);

                    popover.Open(new ToolkitCardModel { title = "Heal", description = "Heals for 45" });
                    yield return null;
                    yield return null;

                    Rect card = root.Q("detail-panel").worldBound;
                    Assert.IsFalse(float.IsNaN(card.yMin) || float.IsNaN(card.xMin));
                    Assert.That(card.yMin, Is.GreaterThanOrEqualTo(root.worldBound.yMin));
                    Assert.That(card.yMax, Is.LessThanOrEqualTo(root.worldBound.yMax));
                    Assert.That(card.xMin, Is.GreaterThanOrEqualTo(root.worldBound.xMin));
                    Assert.That(card.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax));
                    VisualElement hud = root.Q("hud-root");
                    Assert.AreSame(root.Q("detail-panel"), hud.Children().Last(), "Drawn over the class screen, not under it.");
                    popover.Close();
                }

                view.Release();
            }
        }
    }
}
