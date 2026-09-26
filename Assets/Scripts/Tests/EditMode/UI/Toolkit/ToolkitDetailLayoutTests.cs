using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitDetailLayoutTests
    {
        ToolkitTestPanel _panel;
        VisualElement _root;
        ToolkitGameView _view;

        [SetUp]
        public void SetUp()
        {
            _panel = new ToolkitTestPanel();
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(_root);
            _view = new ToolkitGameView(_root);
            _view.SetCards("spell-list", new[]
            {
                new ToolkitCardModel { title = "Heal", status = "5 mana · Ready" },
                new ToolkitCardModel { title = "Heal group", status = "10 mana · Ready" },
                new ToolkitCardModel { title = "Buff attack speed", status = "Free · Ready" },
            });
            _view.ShowDetail(new ToolkitCardModel
            {
                title = "Normal",
                description = "The most basic entity, well balanced",
            });
            _root.Q("detail-icon").AddToClassList("creature-portrait");
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
            _panel.Dispose();
        }

        [UnityTest]
        public IEnumerator Layout_ForestLandscape_ShowsCreatureSummaryBeforeScrolling()
        {
            yield return CheckLandscape(null);
        }

        [UnityTest]
        public IEnumerator Layout_MoonLandscape_ShowsCreatureSummaryBeforeScrolling()
        {
            yield return CheckLandscape(Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme"));
        }

        [UnityTest]
        public IEnumerator Layout_PortraitAndDesktop_KeepsPortraitAboveText()
        {
            ToolkitTheme.Apply(_root, null);
            foreach (Vector2 size in new[] { new Vector2(390f, 844f), new Vector2(1440f, 900f) })
            {
                Resize(size.x, size.y);
                yield return null;
                yield return null;
                Rect portrait = _root.Q("detail-icon").worldBound;
                Rect title = _root.Q("detail-title").worldBound;
                Assert.LessOrEqual(portrait.yMax, title.yMin, "Stacked detail layout at " + size);
                AssertVisible(_root.Q("detail-title"));
            }
        }

        IEnumerator CheckLandscape(ThemeStyleSheet theme)
        {
            ToolkitTheme.Apply(_root, theme);
            Resize(844f, 390f);
            yield return null;
            yield return null;
            VisualElement portrait = _root.Q("detail-icon");
            Label title = _root.Q<Label>("detail-title");
            Label description = _root.Q<Label>("detail-description");
            Assert.LessOrEqual(portrait.worldBound.xMax, title.worldBound.xMin);
            AssertVisible(portrait);
            AssertVisible(title);
            AssertVisible(description);

            description.text = "Health: 100 / 100";
            for (int i = 0; i < 20; i++)
            {
                description.text += "\nCreature attribute: 25";
            }

            yield return null;
            yield return null;
            ScrollView scroll = _root.Q<ScrollView>("detail-scroll");
            Assert.Greater(scroll.verticalScroller.highValue, 0f, "Long stats must remain scrollable.");
            scroll.scrollOffset = new Vector2(0f, scroll.verticalScroller.highValue);
            yield return null;
            yield return null;
            AssertVisible(_root.Q("detail-equip-button"));
        }

        void Resize(float width, float height)
        {
            _root.style.width = width;
            _root.style.height = height;
            ToolkitResponsiveLayout.Apply(_view, width, height);
            _root.Q("hud-root").AddToClassList("detail-open");
        }

        void AssertVisible(VisualElement element)
        {
            Rect bounds = element.worldBound;
            Rect viewport = _root.Q<ScrollView>("detail-scroll").contentViewport.worldBound;
            Rect drawer = _root.Q("detail-panel").worldBound;
            Rect root = _root.worldBound;
            string diagnostic = element.name + ": " + bounds + "; viewport: " + viewport;
            Assert.Greater(bounds.width, 0f, diagnostic);
            Assert.Greater(bounds.height, 0f, diagnostic);
            foreach (Rect clip in new[] { viewport, drawer, root })
            {
                Assert.GreaterOrEqual(bounds.xMin, clip.xMin - 1f, diagnostic);
                Assert.GreaterOrEqual(bounds.yMin, clip.yMin - 1f, diagnostic);
                Assert.LessOrEqual(bounds.xMax, clip.xMax + 1f, diagnostic);
                Assert.LessOrEqual(bounds.yMax, clip.yMax + 1f, diagnostic);
            }
        }
    }
}
