using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitPopoverTests
    {
        [Test]
        public void PersistentDetailsRefreshHealthWithoutFollowingAnotherWorldSelection()
        {
            GameObject owner = new GameObject("Live inspected creature"); owner.SetActive(false);
            GameObject other = new GameObject("Other selection"); other.SetActive(false);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            ToolkitGameView view = new ToolkitGameView(root);
            using (ToolkitDetailPanel details = new ToolkitDetailPanel())
            {
                try
                {
                    Entity entity = null;
                    TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
                    entity.data = data;
                    entity.attributeManager = owner.GetComponent<AttributeManager>();
                    entity.attributeManager.Add(AttributeType.HealthMax, new Attribute(100));
                    entity.attributeManager.Add(AttributeType.Damage, new Attribute(12));
                    entity.attributeManager.Add(AttributeType.CriticalChanceResist, new Attribute(0));
                    ResourceAttribute health = owner.AddComponent<ResourceAttribute>(); health.Init(AttributeType.HealthMax);
                    TestHelpers.SetPrivateField(entity, "_health", health);
                    GameView legacy = owner.AddComponent<GameView>();
                    ToolkitGameContext context = new ToolkitGameContext { legacy = legacy };
                    details.Init(context, view);
                    view.OnInspect.AddListener(details.OnInspect);
                    view.OnInspectEnded.AddListener(details.OnInspectEnded);
                    using (ToolkitPopover popover = new ToolkitPopover(view))
                    {
                        popover.Open(new ToolkitCardModel { source = entity });
                        // A hold release deliberately leaves the popover open, followed by 100 ms refreshes.
                        TestHelpers.SetPrivateField(legacy, "_selectedPanel", PanelType.Entity);
                        TestHelpers.SetPrivateField(legacy, "_selectedObject", other);
                        TestHelpers.SetPrivateField(health, "_value", 63f);
                        details.Refresh(); details.Refresh();
                        Assert.That(popover.isOpen, Is.True);
                        Assert.That(context.selectedEntity, Is.SameAs(entity));
                        string summary = root.Q<Label>("detail-description").text;
                        Assert.That(summary, Does.Contain("63 / 100"));
                        Assert.That(summary, Does.Contain("Damage: 12"));
                        Assert.That(summary, Does.Not.Contain("Maximum health").And.Not.Contain("Critical"));
                        Assert.That(root.Q<Label>("detail-full-stats").text,
                            Does.Contain("Max HP: <b>100</b>").And.Contain("Critical Resist: <b>0</b>"));
                        Foldout expanded = root.Q<Foldout>("detail-attributes");
                        Assert.That(expanded.value, Is.False);
                        expanded.value = true; details.Refresh();
                        Assert.That(expanded.value, Is.True, "Live refresh preserves the expanded state");
                        popover.Close();
                        Assert.That(context.isInspecting, Is.False);
                        popover.Open(new ToolkitCardModel { title = "Spell", description = "Spell description" });
                        details.Refresh();
                        Assert.That(root.Q<Label>("detail-description").text, Is.EqualTo("Spell description"));
                    }
                }
                finally
                {
                    view.Release();
                    Object.DestroyImmediate(owner); Object.DestroyImmediate(other); Object.DestroyImmediate(data);
                }
            }
        }

        [UnityTest]
        public IEnumerator WorldAndRowAnchorsKeepHeaderAndRowsClearWithoutChangingViewport()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                panel.root.Add(root); ToolkitTheme.Apply(root, null);
                ToolkitGameView view = new ToolkitGameView(root);
                using (ToolkitPopover popover = new ToolkitPopover(view))
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
