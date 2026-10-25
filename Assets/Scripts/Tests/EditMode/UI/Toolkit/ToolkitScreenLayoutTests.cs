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

        [Test]
        public void GetBoardViewport_PhoneLandscape_ReachesTowardTheScreenEdgesAndKeepsTheFreeWidth()
        {
            Rect free = Rect.MinMaxRect(0.05f, 0.4f, 0.95f, 0.8f);
            ToolkitLayoutSettings settings = new ToolkitLayoutSettings { landscapeBoardOverlap = 0.5f };
            Rect board = ToolkitScreenLayout.GetBoardViewport(free, ToolkitLayoutMode.Landscape, settings);
            Assert.AreEqual(0.2f, board.yMin, 0.0001f);
            Assert.AreEqual(0.9f, board.yMax, 0.0001f);
            Assert.AreEqual(free.xMin, board.xMin, 0.0001f);
            Assert.AreEqual(free.xMax, board.xMax, 0.0001f);
        }

        [TestCase(0f, 0.4f, 0.8f)]
        [TestCase(1f, 0f, 1f)]
        public void GetBoardViewport_OverlapBounds_GoFromTheFreeAreaToTheFullHeight(float overlap, float yMin,
            float yMax)
        {
            Rect free = Rect.MinMaxRect(0.05f, 0.4f, 0.95f, 0.8f);
            ToolkitLayoutSettings settings = new ToolkitLayoutSettings { landscapeBoardOverlap = overlap };
            Rect board = ToolkitScreenLayout.GetBoardViewport(free, ToolkitLayoutMode.Landscape, settings);
            Assert.AreEqual(yMin, board.yMin, 0.0001f);
            Assert.AreEqual(yMax, board.yMax, 0.0001f);
        }

        [TestCase(ToolkitLayoutMode.Desktop)]
        [TestCase(ToolkitLayoutMode.Portrait)]
        public void GetBoardViewport_OtherLayouts_KeepTheFreeArea(ToolkitLayoutMode mode)
        {
            Rect free = Rect.MinMaxRect(0.05f, 0.35f, 0.95f, 0.8f);
            Assert.AreEqual(free, ToolkitScreenLayout.GetBoardViewport(free, mode));
        }
    }
}
