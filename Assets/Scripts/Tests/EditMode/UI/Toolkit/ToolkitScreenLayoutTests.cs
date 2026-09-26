using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{

public class ToolkitScreenLayoutTests
{
    [TestCase(390, 844)]
    [TestCase(1080, 1920)]
    [TestCase(1170, 2532)]
    public void GetScale_PortraitResolution_KeepsReadablePhoneWidth(int width, int height)
    {
        float scale = ToolkitScreenLayout.GetScale(width, height, false);
        Assert.AreEqual(390f, width / scale, 0.01f);
        Assert.GreaterOrEqual(height / scale, 690f);
    }

    [Test]
    public void GetScale_LandscapePhone_KeepsTouchControlsAtPhoneSize()
    {
        float scale = ToolkitScreenLayout.GetScale(2532, 1170, true);
        Assert.AreEqual(844f, 2532f / scale, 0.01f);
        Assert.AreEqual(390f, 1170f / scale, 0.01f);
    }

    [Test]
    public void GetScale_Desktop_KeepsDesktopLayoutWide()
    {
        float scale = ToolkitScreenLayout.GetScale(1920, 1080, false);
        Assert.Greater(1920f / scale, 1100f);
        Assert.Greater(1080f / scale, 500f);
    }

    [Test]
    public void GetSafePanelRect_NotchAndHomeIndicator_ConvertsBothInsetsToPanelCoordinates()
    {
        Rect safe = ToolkitScreenLayout.GetSafePanelRect(1170, 2532, new Rect(0f, 102f, 1170f, 2289f), 3f);
        Assert.AreEqual(new Rect(0f, 47f, 390f, 763f), safe);
        Assert.AreEqual(34f, 844f - safe.yMax);
    }

    [Test]
    public void GetSafePanelRect_LandscapeNotch_PreservesLeftAndRightInsets()
    {
        Rect safe = ToolkitScreenLayout.GetSafePanelRect(2532, 1170, new Rect(141f, 63f, 2250f, 1107f), 3f);
        Assert.AreEqual(new Rect(47f, 0f, 750f, 369f), safe);
        Assert.AreEqual(47f, 844f - safe.xMax);
    }

    [Test]
    public void GetViewport_HeaderAndDock_ConvertsTopLeftPanelToBottomLeftCamera()
    {
        Rect viewport = ToolkitScreenLayout.GetViewport(
            new Rect(8f, 120f, 374f, 510f),
            new Rect(0f, 0f, 390f, 844f)
        );
        Assert.AreEqual(8f / 390f, viewport.xMin, 0.0001f);
        Assert.AreEqual(214f / 844f, viewport.yMin, 0.0001f);
        Assert.AreEqual(724f / 844f, viewport.yMax, 0.0001f);
    }

    [Test]
    public void GetViewport_DetachedDocument_ReturnsUsableFallback()
    {
        Rect viewport = ToolkitScreenLayout.GetViewport(Rect.zero, Rect.zero);
        Assert.Greater(viewport.width, 0.8f);
        Assert.Greater(viewport.height, 0.5f);
        Assert.Greater(viewport.yMin, 0f);
    }
}
}
