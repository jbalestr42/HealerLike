using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{

    public class ToolkitScreenLayoutTests
    {
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
