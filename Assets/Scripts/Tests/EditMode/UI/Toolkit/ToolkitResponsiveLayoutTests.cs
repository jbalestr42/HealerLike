using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

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

        static VisualElement Apply(float width, float height, bool isTouchInput)
        {
            VisualElement element = new VisualElement();
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(width, height)),
                isTouchInput);
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
            AssertClasses(Apply(1440f, 900f, false), ToolkitResponsiveLayout.DesktopClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.MouseClass);
        }

        [Test]
        public void ApplyClasses_NarrowDesktop_AlsoCompact()
        {
            AssertClasses(Apply(1000f, 700f, false), ToolkitResponsiveLayout.DesktopClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.MouseClass);
        }

        [Test]
        public void ApplyClasses_PhonePortrait_PhonePortraitTouch()
        {
            AssertClasses(Apply(390f, 844f, true), ToolkitResponsiveLayout.PhoneClass,
                ToolkitResponsiveLayout.PortraitClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.TouchClass);
        }

        [Test]
        public void ApplyClasses_PhoneLandscape_PhoneLandscapeTouch()
        {
            AssertClasses(Apply(844f, 390f, true), ToolkitResponsiveLayout.PhoneClass,
                ToolkitResponsiveLayout.LandscapeClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.TouchClass);
        }

        [Test]
        public void ApplyClasses_Rotated_DropsThePreviousLayoutClasses()
        {
            VisualElement element = new VisualElement();
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(390f, 844f)), true);
            ToolkitResponsiveLayout.ApplyClasses(element, ToolkitLayout.FromPanel(new Vector2(1440f, 900f)), false);
            AssertClasses(element, ToolkitResponsiveLayout.DesktopClass, ToolkitResponsiveLayout.LandscapeClass,
                ToolkitResponsiveLayout.MouseClass);
        }

        // A narrow PC window: the portrait layout, with the mouse it is used with
        [Test]
        public void ApplyClasses_PhonePortraitWithMouse_PhonePortraitMouse()
        {
            AssertClasses(Apply(390f, 844f, false), ToolkitResponsiveLayout.PhoneClass,
                ToolkitResponsiveLayout.PortraitClass, ToolkitResponsiveLayout.CompactClass,
                ToolkitResponsiveLayout.MouseClass);
        }

        // The input classes follow the view's last pointer, not the form factor
        [Test]
        public void Apply_MouseUsedOnPhoneLayout_InputMouse()
        {
            VisualElement root = new VisualElement();
            root.Add(new VisualElement { name = "hud-root" });
            ToolkitGameView view = new ToolkitGameView(root);
            ToolkitResponsiveLayout.Apply(view, 390f, 844f);
            Assert.IsTrue(root.Q("hud-root").ClassListContains(ToolkitResponsiveLayout.TouchClass));
            view.RecordPointer(PointerType.mouse);
            ToolkitResponsiveLayout.Apply(view, 390f, 844f);
            Assert.IsTrue(root.Q("hud-root").ClassListContains(ToolkitResponsiveLayout.MouseClass));
            Assert.IsFalse(root.Q("hud-root").ClassListContains(ToolkitResponsiveLayout.TouchClass));
            view.Release();
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
