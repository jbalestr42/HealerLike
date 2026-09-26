using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitPopoverTests
    {
        [UnityTest]
        public IEnumerator WorldAndRowAnchorsKeepHeaderAndRowsClearWithoutChangingViewport()
        {
            using (var panel = new ToolkitTestPanel())
            {
                var root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                panel.root.Add(root); ToolkitTheme.Apply(root, null);
                var view = new ToolkitGameView(root);
                using (var popover = new ToolkitPopover(view))
                {
                    foreach (Vector2 size in new[] { new Vector2(390, 844), new Vector2(844, 390) })
                    {
                        root.style.width = size.x; root.style.height = size.y;
                        ToolkitResponsiveLayout.Apply(view, size.x, size.y);
                        yield return null; yield return null;
                        Rect world = root.Q("world-space").worldBound;
                        foreach (Vector2 anchor in new[] { Vector2.zero, size })
                        {
                            view.inspectAnchor = new Rect(anchor, Vector2.one);
                            popover.Open(new ToolkitCardModel { title = "Creature", description = "Readable details" });
                            yield return null; yield return null;
                            Rect card = root.Q("detail-panel").worldBound;
                            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(root.Q("top-bar").worldBound.yMax));
                            Assert.That(card.yMax, Is.LessThanOrEqualTo(root.Q("party-panel").worldBound.yMin));
                            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(root.worldBound.xMin));
                            Assert.That(card.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax));
                            Assert.That(root.Q("world-space").worldBound, Is.EqualTo(world));
                            popover.Close();
                        }
                    }
                }
                view.Release();
            }
        }
    }
}
