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
                    ResourceAttribute health = owner.AddComponent<ResourceAttribute>(); health.Init(ResourceType.Health, AttributeType.HealthMax, new RawResourceConsumerResolver());
                    TestHelpers.SetPrivateField(entity, "_health", health);
                    InteractionManager interaction = owner.AddComponent<InteractionManager>();
                    ToolkitGameContext context = new ToolkitGameContext { interaction = interaction };
                    details.Init(context, view);
                    view.OnInspect.AddListener(details.OnInspect);
                    view.OnInspectEnded.AddListener(details.OnInspectEnded);
                    using (ToolkitPopover popover = new ToolkitPopover(view))
                    {
                        popover.Open(new ToolkitCardModel { source = entity });
                        // A hold release deliberately leaves the popover open, followed by 100 ms refreshes.
                        TestHelpers.SetPrivateField(interaction, "_selectable", other.AddComponent<SelectableEntity>());
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
                        Assert.That(expanded.value, Is.True, "A unit's details open expanded");
                        foreach (string section in new[] { "detail-combat", "detail-aiming", "detail-skills",
                            "detail-items", "detail-effects" })
                        {
                            Assert.That(root.Q<Foldout>(section).value, Is.True, section + " opens expanded");
                        }

                        expanded.value = false; details.Refresh();
                        Assert.That(expanded.value, Is.False, "Live refresh preserves the collapsed state");
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

        [Test]
        public void UnitDetailsIgnoreTheTapPosition()
        {
            GameObject owner = new GameObject("Tapped unit"); owner.SetActive(false);
            try
            {
                Entity entity = null;
                TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
                Rect tap = new Rect(300, 420, 2, 2);
                Assert.That(ToolkitPopover.GetAnchor(new ToolkitCardModel { source = entity }, tap),
                    Is.EqualTo(default(Rect)));
                Assert.That(ToolkitPopover.GetAnchor(new ToolkitCardModel { source = entity }, new Rect(10, 60, 2, 2)),
                    Is.EqualTo(ToolkitPopover.GetAnchor(new ToolkitCardModel { source = entity }, tap)),
                    "Two taps at different places open at the same place");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void CardsWithoutAUnitKeepTheirAnchor()
        {
            Rect anchor = new Rect(180, 500, 40, 40);
            Assert.That(ToolkitPopover.GetAnchor(new ToolkitCardModel { title = "Spell" }, anchor), Is.EqualTo(anchor));
        }

        [Test]
        public void OpensAboveItsAnchorWhenThereIsRoom()
        {
            Rect area = Rect.MinMaxRect(0, 0, 400, 800);
            Rect anchor = new Rect(180, 500, 40, 40);
            Vector2 position = ToolkitPopover.GetPosition(anchor, new Vector2(200, 100), area);
            Assert.That(position.y + 100, Is.LessThanOrEqualTo(anchor.yMin));
            Assert.That(position.x, Is.EqualTo(100));
        }

        [Test]
        public void OpensBelowAnAnchorWithNoRoomAboveInsteadOfCoveringIt()
        {
            Rect area = Rect.MinMaxRect(0, 60, 400, 800);
            Rect anchor = new Rect(10, 60, 36, 36);
            Vector2 position = ToolkitPopover.GetPosition(anchor, new Vector2(200, 100), area);
            Assert.That(position.y, Is.GreaterThanOrEqualTo(anchor.yMax));
        }

        [Test]
        public void StaysInsideTheAreaWhenThereIsNoRoomAboveOrBelow()
        {
            Rect area = Rect.MinMaxRect(0, 0, 400, 150);
            Rect anchor = new Rect(380, 50, 40, 40);
            Vector2 position = ToolkitPopover.GetPosition(anchor, new Vector2(200, 100), area);
            Assert.That(position.x, Is.EqualTo(200));
            Assert.That(position.y, Is.InRange(0f, 50f));
        }

        [Test]
        public void GetSize_RoomyArea_KeepsThePreferredSize()
        {
            Vector2 size = ToolkitPopover.GetSize(new Vector2(420, 600), Rect.MinMaxRect(0, 90, 1600, 800));
            Assert.That(size, Is.EqualTo(new Vector2(420, 600)));
        }

        [Test]
        public void GetSize_SmallArea_ShrinksToItButNotUnderAReadableHeight()
        {
            Assert.That(ToolkitPopover.GetSize(new Vector2(420, 600), Rect.MinMaxRect(0, 0, 300, 400)),
                Is.EqualTo(new Vector2(300, 400)));
            Assert.That(ToolkitPopover.GetSize(new Vector2(420, 600), Rect.MinMaxRect(0, 0, 300, 40)).y,
                Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator HoveredItemOfTheItemBarIsNotCoveredByItsPopover()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                panel.root.Add(root); ToolkitTheme.Apply(root, null);
                ToolkitGameView view = new ToolkitGameView(root);
                VisualElement chip = new VisualElement();
                chip.style.width = 36; chip.style.height = 36;
                root.Q(ToolkitItemBar.BarName).Add(chip);
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    foreach (Vector2 size in new[] { new Vector2(390, 844), new Vector2(1280, 720) })
                    {
                        root.style.width = size.x; root.style.height = size.y;
                        ToolkitResponsiveLayout.Apply(view, size.x, size.y);
                        yield return null; yield return null;
                        view.inspectAnchor = chip.worldBound;
                        popover.Open(new ToolkitCardModel { title = "Item", description = "Item description" });
                        yield return null; yield return null;
                        Assert.That(root.Q("detail-panel").worldBound.Overlaps(chip.worldBound), Is.False, size.ToString());
                        popover.Close();
                    }
                }
                view.Release();
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

        // Placing the panel moves it, and on a scaled screen the layout rounds that move to the pixel grid: a move
        // placing it again never settles ("Layout update failed to stabilize"). Only a new size places it again.
        [UnityTest]
        public IEnumerator OnlyANewSizePlacesThePanelAgain()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                root.style.width = 390; root.style.height = 693;
                panel.root.Add(root); ToolkitTheme.Apply(root, null);
                ToolkitGameView view = new ToolkitGameView(root);
                ToolkitResponsiveLayout.Apply(view, 390, 693);
                VisualElement detail = root.Q("detail-panel");
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    yield return null; yield return null;
                    view.inspectAnchor = new Rect(200, 300, 2, 2);
                    popover.Open(new ToolkitCardModel { title = "Creature", description = "Readable details" });
                    yield return null; yield return null;
                    float placed = detail.style.top.value.value;
                    detail.style.top = placed + 0.3f;
                    SendGeometry(detail, new Rect(10, 100, 280, 200), new Rect(10, 100.3f, 280, 200));
                    Assert.That(detail.style.top.value.value, Is.EqualTo(placed + 0.3f), "A move is left alone");
                    detail.style.top = placed + 40;
                    SendGeometry(detail, new Rect(10, 100, 280, 200), new Rect(10, 100, 280, 150));
                    Assert.That(detail.style.top.value.value, Is.EqualTo(placed).Within(0.5f), "A new size places it");
                    popover.Close();
                }
                view.Release();
            }
        }

        // The default theme gives the slider an arrow-sized margin and thickness: hidden arrows left the bar short of
        // both ends and wider than its column, drawn over the panel's border
        [UnityTest]
        public IEnumerator ScrollbarStaysInsideThePanelUnderTheCloseButton()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.flexShrink = 0;
                panel.root.Add(root); ToolkitTheme.Apply(root, null);
                ToolkitGameView view = new ToolkitGameView(root);
                using (ToolkitPopover popover = new ToolkitPopover(view))
                {
                    foreach (Vector2 size in new[] { new Vector2(390, 844), new Vector2(1280, 720) })
                    {
                        root.style.width = size.x; root.style.height = size.y;
                        ToolkitResponsiveLayout.Apply(view, size.x, size.y);
                        yield return null; yield return null;
                        view.inspectAnchor = new Rect(size.x * 0.5f, size.y * 0.5f, 2, 2);
                        ToolkitCardModel details = new ToolkitCardModel { title = "Creature",
                            description = string.Join("\n", System.Linq.Enumerable.Repeat("A long line of details", 80)) };
                        // The detail panel fills the popover in game; the test has none
                        view.ShowDetail(details);
                        popover.Open(details);
                        yield return null; yield return null;
                        Rect card = root.Q("detail-panel").worldBound;
                        Scroller scroller = root.Q<ScrollView>("detail-scroll").verticalScroller;
                        Rect bar = scroller.worldBound;
                        Rect slider = scroller.slider.worldBound;
                        Assert.That(scroller.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Overflowing text scrolls at " + size);
                        Assert.That(slider.xMin, Is.GreaterThanOrEqualTo(bar.xMin - 0.5f), size.ToString());
                        Assert.That(slider.xMax, Is.LessThanOrEqualTo(bar.xMax + 0.5f), size.ToString());
                        Assert.That(slider.yMin, Is.EqualTo(bar.yMin).Within(0.5f), "Top end at " + size);
                        Assert.That(slider.yMax, Is.EqualTo(bar.yMax).Within(0.5f), "Bottom end at " + size);
                        Assert.That(bar.xMax, Is.LessThanOrEqualTo(card.xMax - root.Q("detail-panel").resolvedStyle.borderRightWidth),
                            "Inside the border at " + size);
                        Assert.That(bar.yMin, Is.GreaterThanOrEqualTo(root.Q("detail-close-button").worldBound.yMax - 1f),
                            "Under the close button at " + size);
                        popover.Close();
                    }
                }
                view.Release();
            }
        }

        static void SendGeometry(VisualElement element, Rect oldRect, Rect newRect)
        {
            using (GeometryChangedEvent evt = GeometryChangedEvent.GetPooled(oldRect, newRect))
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }

        [Test]
        public void LayoutChanged_OpenPopover_Closes()
        {
            TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            ToolkitGameView view = new ToolkitGameView(root);
            using (ToolkitPopover popover = new ToolkitPopover(view))
            {
                view.SetLayout(ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
                popover.Open(new ToolkitCardModel { title = "Spell", description = "Spell description" });
                Assert.IsTrue(popover.isOpen);
                view.SetLayout(ToolkitLayout.FromPanel(new Vector2(844f, 390f)));
                Assert.IsFalse(popover.isOpen);
            }

            view.Release();
        }

        [Test]
        public void LayoutUnchanged_OpenPopover_StaysOpen()
        {
            TemplateContainer root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            ToolkitGameView view = new ToolkitGameView(root);
            using (ToolkitPopover popover = new ToolkitPopover(view))
            {
                ToolkitLayoutState phone = ToolkitLayout.FromPanel(new Vector2(390f, 844f));
                view.SetLayout(phone);
                popover.Open(new ToolkitCardModel { title = "Spell", description = "Spell description" });
                view.SetLayout(phone);
                Assert.IsTrue(popover.isOpen);
            }

            view.Release();
        }
    }
}
