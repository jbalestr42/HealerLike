using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitThemeTests
    {
        [Test]
        public void Resolve_ExplicitOrMissingChoice_UsesSelectedOrDefaultAsset()
        {
            ThemeStyleSheet forest = Resources.Load<ThemeStyleSheet>("UI/Toolkit/RuntimeTheme");
            ThemeStyleSheet moon = Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme");
            Assert.IsNotNull(forest);
            Assert.IsNotNull(moon);
            Assert.AreSame(forest, ToolkitTheme.Resolve(null));
            Assert.AreSame(moon, ToolkitTheme.Resolve(moon));
        }

        [Test]
        public void Apply_ThemeChangedBackOnSameRoot_ReplacesMoodAndKeepsOtherStyles()
        {
            VisualElement root = new VisualElement();
            StyleSheet local = ScriptableObject.CreateInstance<StyleSheet>();
            ThemeStyleSheet forest = ToolkitTheme.Resolve(null);
            ThemeStyleSheet moon = Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme");
            try
            {
                root.styleSheets.Add(local);
                Assert.IsTrue(ToolkitTheme.Apply(root, null));
                Assert.IsTrue(ToolkitTheme.Apply(root, moon));
                Assert.IsFalse(root.styleSheets.Contains(forest));
                Assert.IsTrue(root.styleSheets.Contains(moon));
                Assert.IsTrue(ToolkitTheme.Apply(root, null));
                Assert.IsFalse(root.styleSheets.Contains(moon));
                Assert.IsTrue(root.styleSheets.Contains(forest));
                Assert.IsTrue(root.styleSheets.Contains(local));
                Assert.AreEqual(2, root.styleSheets.count);
            }
            finally
            {
                Object.DestroyImmediate(local);
            }
        }

        [Test]
        public void CreatePanelSettings_SourceThemeDiffers_UsesSameChoiceAsPreviewWithoutMutatingSource()
        {
            PanelSettings source = ScriptableObject.CreateInstance<PanelSettings>();
            ThemeStyleSheet moon = Resources.Load<ThemeStyleSheet>("UI/Toolkit/MoonTheme");
            source.themeStyleSheet = moon;
            PanelSettings inherited = ToolkitTheme.CreatePanelSettings(source, null);
            PanelSettings selected = ToolkitTheme.CreatePanelSettings(source, moon);
            try
            {
                VisualElement preview = new VisualElement();
                ToolkitTheme.Apply(preview, moon);
                Assert.AreSame(moon, source.themeStyleSheet);
                Assert.AreSame(ToolkitTheme.Resolve(null), inherited.themeStyleSheet);
                Assert.AreSame(moon, selected.themeStyleSheet);
                Assert.AreSame(preview.styleSheets[0], selected.themeStyleSheet);
                Assert.AreEqual(PanelScaleMode.ConstantPixelSize, selected.scaleMode);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(inherited);
                Object.DestroyImmediate(selected);
            }
        }

        [UnityTest]
        public IEnumerator ResolveStyles_PhoneInfoButton_PreservesCompactCardOverride()
        {
            using (ToolkitTestPanel panel = new ToolkitTestPanel())
            {
                VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                root.style.width = 390f;
                root.style.height = 844f;
                ToolkitTheme.Apply(root, null);
                panel.root.Add(root);
                ToolkitGameView view = new ToolkitGameView(root);
                try
                {
                    ToolkitResponsiveLayout.Apply(view, 390f, 844f);
                    view.SetCards("spell-list", new[] { new ToolkitCardModel() });
                    yield return null;
                    yield return null;
                    Button info = root.Q("spell-list").Q<Button>("card-info");
                    Assert.AreEqual(45f, info.resolvedStyle.width);
                    Assert.AreEqual(45f, info.resolvedStyle.height);
                    Assert.AreEqual(11f, info.resolvedStyle.fontSize);
                    Assert.AreEqual(0f, info.resolvedStyle.paddingLeft);
                    Assert.AreEqual(0f, info.resolvedStyle.paddingRight);
                    Assert.AreEqual(0f, info.resolvedStyle.paddingTop);
                    Assert.AreEqual(0f, info.resolvedStyle.paddingBottom);
                }
                finally
                {
                    view.Release();
                }
            }
        }
    }
}
