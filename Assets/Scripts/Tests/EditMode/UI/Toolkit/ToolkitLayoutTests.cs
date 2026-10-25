using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitLayoutTests
    {
        static ToolkitLayoutState Resolve(int width, int height, bool isMobilePlatform)
        {
            return ToolkitLayout.Resolve(new Vector2Int(width, height), new Rect(0f, 0f, width, height),
                isMobilePlatform);
        }

        [TestCase(390, 844)]
        [TestCase(1080, 1920)]
        [TestCase(1170, 2532)]
        public void GetScale_PortraitResolution_KeepsReadablePhoneWidth(int width, int height)
        {
            float scale = ToolkitLayout.GetScale(width, height, false);
            Assert.AreEqual(390f, width / scale, 0.01f);
            Assert.GreaterOrEqual(height / scale, 690f);
        }

        [Test]
        public void GetScale_LandscapePhone_KeepsTouchControlsAtPhoneSize()
        {
            float scale = ToolkitLayout.GetScale(2532, 1170, true);
            Assert.AreEqual(844f, 2532f / scale, 0.01f);
            Assert.AreEqual(390f, 1170f / scale, 0.01f);
        }

        [Test]
        public void GetScale_Desktop_KeepsDesktopLayoutWide()
        {
            float scale = ToolkitLayout.GetScale(1920, 1080, false);
            Assert.Greater(1920f / scale, 1100f);
            Assert.Greater(1080f / scale, 500f);
        }

        [Test]
        public void GetScale_EmptyScreen_StaysUsable()
        {
            Assert.AreEqual(1f, ToolkitLayout.GetScale(0, 0, true));
        }

        [Test]
        public void GetSafePanelRect_NotchAndHomeIndicator_ConvertsBothInsetsToPanelCoordinates()
        {
            Rect safe = ToolkitLayout.GetSafePanelRect(1170, 2532, new Rect(0f, 102f, 1170f, 2289f), 3f);
            Assert.AreEqual(new Rect(0f, 47f, 390f, 763f), safe);
            Assert.AreEqual(34f, 844f - safe.yMax);
        }

        [Test]
        public void GetSafePanelRect_LandscapeNotch_PreservesLeftAndRightInsets()
        {
            Rect safe = ToolkitLayout.GetSafePanelRect(2532, 1170, new Rect(141f, 63f, 2250f, 1107f), 3f);
            Assert.AreEqual(new Rect(47f, 0f, 750f, 369f), safe);
            Assert.AreEqual(47f, 844f - safe.xMax);
        }

        [TestCase(1080, 1920, true)]
        [TestCase(1170, 2532, true)]
        // A PC window taller than wide takes the portrait layout too
        [TestCase(900, 1600, false)]
        public void Resolve_TallerThanWide_IsPortrait(int width, int height, bool isMobilePlatform)
        {
            ToolkitLayoutState state = Resolve(width, height, isMobilePlatform);
            Assert.AreEqual(ToolkitLayoutMode.Portrait, state.mode);
            Assert.IsTrue(state.isPhone);
            Assert.IsFalse(state.isLandscape);
            Assert.AreEqual(390f, state.panelSize.x, 0.01f);
        }

        [TestCase(2532, 1170, true)]
        [TestCase(2400, 1080, true)]
        // A PC window too short for the desktop layout gets the phone one
        [TestCase(1280, 500, false)]
        public void Resolve_PhoneLandscape_IsLandscape(int width, int height, bool isMobilePlatform)
        {
            ToolkitLayoutState state = Resolve(width, height, isMobilePlatform);
            Assert.AreEqual(ToolkitLayoutMode.Landscape, state.mode);
            Assert.IsTrue(state.isPhone);
            Assert.IsTrue(state.isLandscape);
            Assert.AreEqual(390f, state.panelSize.y, 0.01f);
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        [TestCase(2560, 1440)]
        public void Resolve_PcScreen_IsWideDesktop(int width, int height)
        {
            ToolkitLayoutState state = Resolve(width, height, false);
            Assert.AreEqual(ToolkitLayoutMode.Desktop, state.mode);
            Assert.IsFalse(state.isPhone);
            Assert.IsFalse(state.isCompact);
            Assert.AreEqual(900f, state.panelSize.y, 0.01f);
        }

        [Test]
        public void Resolve_SmallPcWindow_IsCompactDesktop()
        {
            ToolkitLayoutState state = Resolve(800, 600, false);
            Assert.AreEqual(ToolkitLayoutMode.Desktop, state.mode);
            Assert.IsTrue(state.isCompact);
        }

        [Test]
        public void Resolve_Notch_KeepsTheSafeAreaInPanelUnits()
        {
            ToolkitLayoutState state = ToolkitLayout.Resolve(new Vector2Int(1170, 2532),
                new Rect(0f, 102f, 1170f, 2289f), true);
            Assert.AreEqual(3f, state.scale, 0.0001f);
            Assert.AreEqual(new Rect(0f, 47f, 390f, 763f), state.safeRect);
        }

        [Test]
        public void Resolve_Rotation_ChangesTheStateAndBackRestoresIt()
        {
            ToolkitLayoutState portrait = Resolve(1080, 2400, true);
            ToolkitLayoutState landscape = Resolve(2400, 1080, true);
            Assert.AreNotEqual(portrait, landscape);
            Assert.AreEqual(portrait, Resolve(1080, 2400, true));
        }

        [Test]
        public void Resolve_EmptyScreen_DoesNotProduceInvalidSizes()
        {
            ToolkitLayoutState state = Resolve(0, 0, true);
            Assert.AreEqual(Vector2.zero, state.panelSize);
            Assert.IsFalse(float.IsNaN(state.safeRect.width));
        }

        [TestCase(390f, 844f, ToolkitLayoutMode.Portrait)]
        [TestCase(844f, 390f, ToolkitLayoutMode.Landscape)]
        [TestCase(1440f, 900f, ToolkitLayoutMode.Desktop)]
        [TestCase(1280f, 720f, ToolkitLayoutMode.Desktop)]
        public void FromPanel_PreviewSizes_MatchTheScreenLayouts(float width, float height, ToolkitLayoutMode mode)
        {
            ToolkitLayoutState state = ToolkitLayout.FromPanel(new Vector2(width, height));
            Assert.AreEqual(mode, state.mode);
            Assert.AreEqual(1f, state.scale);
            Assert.AreEqual(new Rect(0f, 0f, width, height), state.safeRect);
        }

        [Test]
        public void FromPanel_CustomSettings_MoveTheThresholds()
        {
            ToolkitLayoutSettings settings = new ToolkitLayoutSettings { landscapeMaxHeight = 800f, compactWidth = 500f };
            ToolkitLayoutState state = ToolkitLayout.FromPanel(new Vector2(1280f, 720f), settings);
            Assert.AreEqual(ToolkitLayoutMode.Landscape, state.mode);
            Assert.IsFalse(state.isCompact);
        }
    }
}
