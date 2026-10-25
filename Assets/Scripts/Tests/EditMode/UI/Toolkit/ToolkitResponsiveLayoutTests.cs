using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    public class ToolkitResponsiveLayoutTests
    {
        static readonly string[] AllClasses =
        {
            ToolkitResponsiveLayout.DesktopClass,
            ToolkitResponsiveLayout.PhoneClass,
            ToolkitResponsiveLayout.PortraitClass,
            ToolkitResponsiveLayout.LandscapeClass,
            ToolkitResponsiveLayout.CompactClass,
            ToolkitResponsiveLayout.MouseClass,
            ToolkitResponsiveLayout.TouchClass,
        };

        static VisualElement Apply(float width, float height)
        {
            VisualElement element = new VisualElement();
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(width, height)));
            return element;
        }

        static void AssertClasses(VisualElement element, params string[] expected)
        {
            foreach (string name in AllClasses)
            {
                bool shouldHave = System.Array.IndexOf(expected, name) >= 0;
                Assert.AreEqual(shouldHave, element.ClassListContains(name), name);
            }
        }

        [Test]
        public void ApplyClasses_WideDesktop_DesktopLandscapeMouse()
        {
            AssertClasses(Apply(1440f, 900f), ToolkitResponsiveLayout.DesktopClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.MouseClass);
        }

        [Test]
        public void ApplyClasses_NarrowDesktop_AlsoCompact()
        {
            AssertClasses(Apply(1000f, 700f), ToolkitResponsiveLayout.DesktopClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.MouseClass);
        }

        [Test]
        public void ApplyClasses_PhonePortrait_PhonePortraitTouch()
        {
            AssertClasses(Apply(390f, 844f), ToolkitResponsiveLayout.PhoneClass,
                ToolkitResponsiveLayout.PortraitClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.TouchClass);
        }

        [Test]
        public void ApplyClasses_PhoneLandscape_PhoneLandscapeTouch()
        {
            AssertClasses(Apply(844f, 390f), ToolkitResponsiveLayout.PhoneClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.TouchClass);
        }

        [Test]
        public void ApplyClasses_Rotated_DropsThePreviousLayoutClasses()
        {
            VisualElement element = new VisualElement();
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(390f, 844f)));
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(1440f, 900f)));
            AssertClasses(element, ToolkitResponsiveLayout.DesktopClass, ToolkitResponsiveLayout.LandscapeClass,
                ToolkitResponsiveLayout.MouseClass);
        }

        [Test]
        public void Apply_SameSizeEveryFrame_AnnouncesOnlyTheChange()
        {
            VisualElement root = new VisualElement();
            root.Add(new VisualElement { name = "hud-root" });
            ToolkitGameView view = new ToolkitGameView(root);
            int changes = 0;
            view.OnLayoutChanged += delegate { changes++; };
            ToolkitResponsiveLayout.Apply(view, 390f, 844f);
            ToolkitResponsiveLayout.Apply(view, 390f, 844f);
            ToolkitResponsiveLayout.Apply(view, 1440f, 900f);
            Assert.AreEqual(2, changes);
            Assert.IsTrue(root.Q("hud-root").ClassListContains(ToolkitResponsiveLayout.DesktopClass));
            view.Release();
        }
    }
}
